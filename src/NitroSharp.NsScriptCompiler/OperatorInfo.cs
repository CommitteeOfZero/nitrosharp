namespace NitroSharp.NsScript
{
    public static class OperatorInfo
    {
        public static string GetText(BinaryOperatorKind operatorKind)
        {
            return operatorKind switch
            {
                BinaryOperatorKind.Add => "+",
                BinaryOperatorKind.Subtract => "-",
                BinaryOperatorKind.Multiply => "*",
                BinaryOperatorKind.Divide => "/",
                BinaryOperatorKind.Remainder => "%",
                BinaryOperatorKind.Equals => "==",
                BinaryOperatorKind.NotEquals => "!=",
                BinaryOperatorKind.LessThan => "<",
                BinaryOperatorKind.LessThanOrEqual => "<=",
                BinaryOperatorKind.GreaterThan => ">",
                BinaryOperatorKind.GreaterThanOrEqual => ">=",
                BinaryOperatorKind.And => "&&",
                BinaryOperatorKind.Or => "||",

                _ => throw ThrowHelper.UnexpectedValue(nameof(operatorKind)),
            };
        }

        public static string GetText(AssignmentOperatorKind operatorKind)
        {
            return operatorKind switch
            {
                AssignmentOperatorKind.Assign => "=",
                AssignmentOperatorKind.AddAssign => "+=",
                AssignmentOperatorKind.SubtractAssign => "-=",
                AssignmentOperatorKind.MultiplyAssign => "*=",
                AssignmentOperatorKind.DivideAssign => "/=",
                AssignmentOperatorKind.Increment => "++",
                AssignmentOperatorKind.Decrement => "--",

                _ => throw ThrowHelper.UnexpectedValue(nameof(operatorKind)),
            };
        }

        public static string GetText(UnaryOperatorKind operatorKind)
        {
            return operatorKind switch
            {
                UnaryOperatorKind.Not => "!",
                UnaryOperatorKind.Plus => "+",
                UnaryOperatorKind.Minus => "-",

                _ => throw ThrowHelper.UnexpectedValue(nameof(operatorKind)),
            };
        }
    }
}
