using System;
using System.Collections.Generic;
using System.Linq;
using SharpKit.Targets.Java.Ast;

namespace SharpKit.Targets.Java
{
    class JYieldRefactorer
    {
        public JFunction BeforeFunction { get; set; }
        public JFunction AfterFunction { get; set; }

        Dictionary<JNode, JNode> Parents = new Dictionary<JNode, JNode>();
        void SetParents(JNode node)
        {
            foreach (var ch in node.Children())
            {
                Parents[ch] = node;
                SetParents(ch);
            }
        }
        JNode GetParent(JNode node)
        {
            return Parents.TryGetValue(node);
        }
        public void Process()
        {
            AfterFunction = BeforeFunction;
            SetParents(BeforeFunction.Block);
            foreach (var me in BeforeFunction.Block.Descendants<JMemberExpression>().ToList())
            {
                if (me.PreviousMember == null && me is JMemberExpression)
                    me.PreviousMember = J.This();
            }

            BeginNewStep();
            ProcessStatement(BeforeFunction.Block);
            BeforeFunction.Block.Statements.Clear();

            var func = new JFunction { Block = new JBlock { Statements = new List<JStatement>() } };
            var i = 0;
            func.Block.Statements.Add(J.Var("result", null).Statement());
            var stSwitch = J.Switch(_state());
            var lastStep = J.Block().Add(_state().Assign(J.Value(Steps.Count)).Statement()).Add(new JBreakStatement());
            Steps.Add(new JYieldStep { Statements = { lastStep } });
            foreach (var step in Steps)
            {
                stSwitch.Case(J.Value(i), step.Statements);
                i++;
            }
            func.Block.Statements.Add(stSwitch);
            func.Block.Statements.Add(J.Member("result").Assign(J.Value(false)).Statement());
            func.Block.Statements.Add(J.Return(J.Member("result")));
            throw new NotSupportedException();
            //BeforeFunction.Block.Statements.Add(J.Return(J.New(J.Member("CustomEnumerable"), func)));
            //return;
        }

        void BeginNewStep()
        {
            Steps.Add(new JYieldStep());
            AddToCurrentStep(_state().Assign(J.Value(-1)).Statement());
        }

        void AddToCurrentStep(JStatement st)
        {
            Steps.Last().Statements.Add(st);
        }

        void YieldReturnInCurrentStep(JExpression item)
        {
            var result = J.Value(true);
            var stepIndex = Steps.Count - 1;
            AddToCurrentStep(J.This().Member("_current").Assign(item).Statement());
            AddToCurrentStep(_state().Assign(J.Value(stepIndex + 1)).Statement());
            AddToCurrentStep(J.Member("result").Assign(result).Statement());
            AddToCurrentStep(J.Return(result));
        }


        object ProcessStatement(JStatement node)
        {
            if (node is JYieldReturnStatement)
            {
                var st2 = (JYieldReturnStatement)node;
                YieldReturnInCurrentStep(st2.Expression);
                BeginNewStep();
                return null;
            }
            else if (node is JVariableDeclarationStatement)
            {
                var node2 = (JVariableDeclarationStatement)node;
                var decl = node2.Declaration.Declarators.Single();
                var node3 = J.This().Member(decl.Name).Assign(decl.Initializer).Statement();
                AddToCurrentStep(node3);
                return null;
            }

            if (node is JBlock)
            {
                //BeginNewStep();
                var block = (JBlock)node;
                foreach (var st in block.Statements)
                {
                    ProcessStatement(st);
                }
            }
            else if (node is JWhileStatement)
            {
                BeginNewStep();
                var st = (JWhileStatement)node;
                ProcessStatement(st.Statement);
                var step = Steps.Last();
                st.Statement = new JBlock { Statements = step.Statements.ToList() };
                step.Statements.Clear();
                step.Statements.Add(st);
                BeginNewStep();
            }
            else
            {
                AddToCurrentStep(node);
            }
            return node;
        }

        private static JMemberExpression _state()
        {
            return J.This().Member("_state");
        }

        List<JYieldStep> Steps = new List<JYieldStep>();

        private void ReplaceNode(JNode node, JNode node2)
        {
            var parent = GetParent(node);
            if (parent is JBlock)
            {
                var block = (JBlock)parent;
                var index = block.Statements.IndexOf((JStatement)node);
                if (index < 0)
                    throw new Exception("ReplaceNode Failed");
                block.Statements[index] = (JStatement)node2;
                return;
            }
            foreach (var pe in parent.GetType().GetProperties())
            {
                var obj = pe.GetValue(parent, null);
                if (obj == node)
                {
                    pe.SetValue(parent, node2, null);
                    return;
                }
            }
            throw new Exception("ReplaceNode failed");
        }
    }

    class JYieldStatement : JStatement
    {
    }
    class JYieldReturnStatement : JYieldStatement
    {
        public JExpression Expression { get; set; }
        public override IEnumerable<JNode> Children()
        {
            if (Expression != null)
                yield return Expression;
        }
    }

    class JYieldBreakStatement : JYieldStatement
    {
    }
    class JYieldStep
    {
        public JYieldStep()
        {
            Statements = new List<JStatement>();
        }
        public List<JStatement> Statements { get; set; }
    }
}
