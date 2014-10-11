using System;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Targets.JavaScript.Ast;
using SharpKit.Targets.Ast;

namespace SharpKit.Targets.Java.Ast
{
    partial class JNode : ITargetNode
    {
        public string ToJ()
        {
            using (var writer = JWriter.CreateInMemory())
            {
                writer.Visit(this);
                return writer.GetStringBuilder().ToString();
            }
        }
        //public JNode Parent { get; set; }
        public object Metadata { get; set; }
        public TextLocation StartLocation { get; set; }
        public TextLocation EndLocation { get; set; }


        public override string ToString()
        {
            try
            {
                return this.GetType().Name + " : " + ToJ();
            }
            catch
            {
                return this.GetType().Name;
            }
        }

    }

    class EntityModifiers
    {
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsProtected { get; set; }
        public bool IsStatic { get; set; }
        public bool IsAbstract { get; set; }
    }

    abstract partial class JEntityDeclaration
    {
        partial void Init()
        {
            Modifiers = new EntityModifiers();
        }

        public EntityModifiers Modifiers { get; set; }
        public abstract IEntity Entity { get; }

    }
    partial class JMethodDeclaration
    {
        public override IEntity Entity { get { return MethodDefinition; } }
    }
    partial class JFieldDeclaration
    {
        public override IEntity Entity { get { return FieldDefinition; } }

    }
    partial class JClassDeclaration
    {
        public override IEntity Entity { get { return TypeDefinition; } }
        public bool IsInterface { get; set; }
    }

    partial class JParameterDeclaration
    {
        public override IEntity Entity { get { return null; } }
        public IParameter Parameter { get; set; }
    }

    partial class JMemberExpression
    {
        public bool IsArray { get; set; }
        public IType TypeRef { get; set; }
    }

    partial class JCodeExpression
    {
        public Action<JWriter> WriteOverride { get; set; }
    }
}
