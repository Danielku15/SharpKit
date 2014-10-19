using System;
using System.Linq.Expressions;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;

namespace SharpKit.Compiler.Utils
{
    public static class Extensions
    {
        public static CSharpInvocationResolveResult ToCSharpInvocationResolveResult(this InvocationResolveResult res)
        {
            if (res == null)
                return null;
            if (res is CSharpInvocationResolveResult)
                return (CSharpInvocationResolveResult)res;
            var res2 = new CSharpInvocationResolveResult(res.TargetResult, res.Member, res.Arguments, initializerStatements: res.InitializerStatements);
            res2.Tag = res.Tag;
            return res2;
        }

    }

    public static class ExpressionExtensions
    {
        public static ExpressionType? ExtractCompoundAssignment(this ExpressionType x)
        {
            switch (x)
            {
                case ExpressionType.PostIncrementAssign: return ExpressionType.AddAssign;
                case ExpressionType.PostDecrementAssign: return ExpressionType.SubtractAssign;
                case ExpressionType.PreIncrementAssign: return ExpressionType.AddAssign;
                case ExpressionType.PreDecrementAssign: return ExpressionType.SubtractAssign;

                case ExpressionType.AddAssign: return ExpressionType.Add;
                case ExpressionType.SubtractAssign: return ExpressionType.Subtract;
                case ExpressionType.DivideAssign: return ExpressionType.Divide;
                case ExpressionType.ModuloAssign: return ExpressionType.Modulo;
                case ExpressionType.MultiplyAssign: return ExpressionType.Multiply;
            }
            return null;
        }
        public static bool IsActionAndAssign(this ExpressionType x)
        {
            switch (x)
            {
                case ExpressionType.PostIncrementAssign: return true;
                case ExpressionType.PostDecrementAssign: return true;
                case ExpressionType.PreIncrementAssign: return true;
                case ExpressionType.PreDecrementAssign: return true;

                case ExpressionType.AddAssign: return true;
                case ExpressionType.SubtractAssign: return true;
                case ExpressionType.DivideAssign: return true;
                case ExpressionType.ModuloAssign: return true;
                case ExpressionType.MultiplyAssign: return true;
            }
            return false;
        }
        public static bool IsAny(this ExpressionType value, params ExpressionType[] values)
        {
            if (values == null)
                return false;
            foreach (var test in values)
                if (value == test)
                    return true;
            return false;
        }
    }


    public class NamespaceVerificationAnnotation
    {
        public string Namespace { get; set; }
    }
}
