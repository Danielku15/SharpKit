using System.Collections.Generic;
using System.Text;
using ICSharpCode.NRefactory.CSharp;

namespace SharpKit.Compiler.Targets.Utils
{
    public class CommentsExporter
    {
        private int _currentTokenIndex = -1;

        public List<AstNode> Nodes { get; set; }

        public List<string> ExportAllLeftoverComments()
        {
            return ExportCommentsUptoNode(null);//MaxLine.GetValueOrDefault(int.MaxValue));
        }

        public List<string> ExportCommentsUptoNode(AstNode stopNode)
        {
            if (_currentTokenIndex == -1)
                _currentTokenIndex = 0;
            var sb = new StringBuilder();
            var started = false;
            var lines = new List<string>();
            while (_currentTokenIndex < Nodes.Count)
            {
                var token = Nodes[_currentTokenIndex];
                if (token == stopNode)
                    break;
                var cmt = token as Comment;
                if (cmt != null)
                {
                    started = true;
                    if (cmt.CommentType == CommentType.SingleLine)
                    {
                        sb.AppendFormat("//{0}", cmt.Content);
                    }
                    else if (cmt.CommentType == CommentType.MultiLine)
                    {
                        sb.AppendFormat("/*{0}*/", cmt.Content);
                    }
                    lines.Add(sb.ToString());
                    sb.Clear();
                }
                _currentTokenIndex++;
            }
            if (started && sb.Length > 0)
            {
                lines.Add(sb.ToString());
            }
            return lines;
        }
    }
}
