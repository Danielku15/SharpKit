using System.Collections.Generic;
using SharpKit.JavaScript.Ast;

namespace SharpKit.Compiler
{
    class CodeInjection
    {
        public CodeInjection()
        {
            Dependencies = new List<CodeInjection>();
        }
        public List<CodeInjection> SelfAndDependencies()
        {
            var list = new List<CodeInjection> { this };
            if (Dependencies.IsNotNullOrEmpty())
            {
                foreach (var dep in Dependencies)
                {
                    list.AddRange(dep.SelfAndDependencies());
                }
            }
            return list;
        }
        public string JsCode { get; set; }
        public string FunctionName { get; set; }
        public JsStatement JsStatement { get; set; }
        public List<CodeInjection> Dependencies { get; set; }
    }
}