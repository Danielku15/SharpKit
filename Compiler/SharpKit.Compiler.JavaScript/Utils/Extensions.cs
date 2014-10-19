using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Compiler.JavaScript.Ast;

namespace SharpKit.Compiler.JavaScript.Utils
{
    static class ExpressionExtensions
    {
        public static string ToJs(this ExpressionType op)
        {
            switch (op)
            {
                case ExpressionType.Add: return "+";
                case ExpressionType.AddAssign: return "+=";
                case ExpressionType.AddAssignChecked: break;
                case ExpressionType.AddChecked: break;
                case ExpressionType.And: return "&";
                case ExpressionType.AndAlso: return "&&";
                case ExpressionType.AndAssign: return "&=";
                case ExpressionType.ArrayIndex: break;
                case ExpressionType.ArrayLength: break;
                case ExpressionType.Assign: return "=";
                case ExpressionType.Block: break;
                case ExpressionType.Call: break;
                case ExpressionType.Coalesce: return "??";
                case ExpressionType.Conditional: break;
                case ExpressionType.Constant: break;
                case ExpressionType.Convert: break;
                case ExpressionType.ConvertChecked: break;
                case ExpressionType.DebugInfo: break;
                case ExpressionType.Decrement: break;
                case ExpressionType.Default: break;
                case ExpressionType.Divide: return "/";
                case ExpressionType.DivideAssign: return "/=";
                case ExpressionType.Dynamic: break;
                case ExpressionType.Equal: return "==";
                case ExpressionType.ExclusiveOr: return "^";
                case ExpressionType.ExclusiveOrAssign: return "^=";
                case ExpressionType.Extension: break;
                case ExpressionType.Goto: break;
                case ExpressionType.GreaterThan: return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                case ExpressionType.Increment: break;
                case ExpressionType.Index: break;
                case ExpressionType.Invoke: break;
                case ExpressionType.IsFalse: break;
                case ExpressionType.IsTrue: break;
                case ExpressionType.Label: break;
                case ExpressionType.Lambda: break;
                case ExpressionType.LeftShift: return "<<";
                case ExpressionType.LeftShiftAssign: return "<<=";
                case ExpressionType.LessThan: return "<";
                case ExpressionType.LessThanOrEqual: return "<=";
                case ExpressionType.ListInit: break;
                case ExpressionType.Loop: break;
                case ExpressionType.MemberAccess: break;
                case ExpressionType.MemberInit: break;
                case ExpressionType.Modulo: return "%";
                case ExpressionType.ModuloAssign: return "%=";
                case ExpressionType.Multiply: return "*";
                case ExpressionType.MultiplyAssign: return "*=";
                case ExpressionType.MultiplyAssignChecked: break;
                case ExpressionType.MultiplyChecked: break;
                case ExpressionType.Negate: return "-";
                case ExpressionType.NegateChecked: break;
                case ExpressionType.New: break;
                case ExpressionType.NewArrayBounds: break;
                case ExpressionType.NewArrayInit: break;
                case ExpressionType.Not: return "!";
                case ExpressionType.NotEqual: return "!=";
                case ExpressionType.OnesComplement: return "~";
                case ExpressionType.Or: return "|";
                case ExpressionType.OrAssign: return "|=";
                case ExpressionType.OrElse: return "||";
                case ExpressionType.Parameter: break;
                case ExpressionType.PostDecrementAssign: return "--";
                case ExpressionType.PostIncrementAssign: return "++";
                case ExpressionType.Power: break;
                case ExpressionType.PowerAssign: break;
                case ExpressionType.PreDecrementAssign: return "--";
                case ExpressionType.PreIncrementAssign: return "++";
                case ExpressionType.Quote: break;
                case ExpressionType.RightShift: return ">>";
                case ExpressionType.RightShiftAssign: return ">>=";
                case ExpressionType.RuntimeVariables: break;
                case ExpressionType.Subtract: return "-";
                case ExpressionType.SubtractAssign: return "-=";
                case ExpressionType.SubtractAssignChecked: break;
                case ExpressionType.SubtractChecked: break;
                case ExpressionType.Switch: break;
                case ExpressionType.Throw: break;
                case ExpressionType.Try: break;
                case ExpressionType.TypeAs: break;
                case ExpressionType.TypeEqual: break;
                case ExpressionType.TypeIs: break;
                case ExpressionType.UnaryPlus: break;
                case ExpressionType.Unbox: break;
                default: break;
            }
            throw new NotImplementedException(op.ToString());
        }
    }

    #region Clr mode

    class VerifyJsTypesArrayStatementAnnotation
    {
    }

    class EntityToJsNode
    {
        public IEntity Entity { get; set; }
        public JsNode JsNode { get; set; }
    }

    /// <summary>
    /// A type used internally by the Js Type System
    /// </summary>
    [JsType(JsMode.Json)]
    class JsClrType
    {
        public JsObject GetDefinition(bool isStatic)
        {
            if (isStatic)
            {
                if (staticDefinition == null)
                    staticDefinition = new JsObject();
                return staticDefinition;
            }
            else
            {
                if (definition == null)
                    definition = new JsObject();
                return definition;
            }
        }
        public string fullname { get; set; }
        public string baseTypeName { get; set; }
        public JsObject definition { get; set; }
        public JsObject staticDefinition { get; set; }
        public bool? isPartial { get; set; }
        public string assemblyName { get; set; }
        public JsArray<JsClrAttribute> customAttributes { get; set; }
        public JsArray<string> interfaceNames { get; set; }
        public JsClrTypeKind Kind { get; set; }
        public JsFunction cctor { get; set; }
    }

    [JsType(JsMode.Json)]
    [JsEnum(ValuesAsNames = true)]
    enum JsClrTypeKind
    {
        Class,
        Struct,
        Interface,
        Enum,
        Delegate,
    }

    [JsType(JsMode.Json)]
    class JsClrAttribute
    {
        public string targetType { get; set; }
        public string targetMemberName { get; set; }
        public string typeName { get; set; }
        public string ctorName { get; set; }
        public JsArray<object> positionalArguments { get; set; }
        public JsObject namedArguments { get; set; }
    }

    class JsObject : Dictionary<string, object>
    {
    }

    class JsArray<T> : List<T>
    {
    }
    #endregion

}
