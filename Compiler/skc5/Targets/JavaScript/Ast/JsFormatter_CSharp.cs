namespace SharpKit.Targets.JavaScript.Ast
{
    class JsFormatter_CSharp : JsFormatter_Default
    {

        protected override void OnBracketOpen()
        {
            //if (Current.Node.Is(JsNodeType.JsonObjectExpression))
            //{
            //}
            AddBeforeCurrent(JsToken.Enter());
            OnFirstVisibleTokenAfterNewLine();
            base.OnBracketOpen();
        }

    }
}
