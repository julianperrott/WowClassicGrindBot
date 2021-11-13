using System.Collections.Generic;

namespace Core
{
    public static class InfixToPostfix
    {
        public static List<string> Convert(string input)
        {
            List<string> output = new List<string>();
            Stack<string> stack = new Stack<string>();

            int i = 0;
            while (input.Length != 0)
            {
                if (IsSpecialChar(input[i]))
                {
                    if (input[i] == '(')
                    {
                        stack.Push(input[i].ToString());
                        input = input[1..];
                        i = 0;
                    }
                    else if (input[i] == ')')
                    {
                        while (stack.Count != 0 && stack.Peek() != "(")
                        {
                            output.Add(stack.Pop());
                        }
                        stack.Pop();
                        input = input[1..];
                        i = 0;
                    }
                    else if (IsOperator(input, i, out string @operator))
                    {
                        input = input[@operator.Length..];

                        while (stack.Count != 0 && OperatorPriority(stack.Peek()) >= OperatorPriority(@operator))
                        {
                            output.Add(stack.Pop());
                        }

                        stack.Push(@operator);
                        i = 0;
                    }
                }
                else
                {
                    while (i < input.Length && !IsSpecialChar(input[i]))
                    {
                        i++;
                    }

                    output.Add(input.Substring(0, i)); // operand
                    input = input[i..];
                    i = 0;
                }
            }

            while (stack.Count != 0)
            {
                output.Add(stack.Pop());
            }

            return output;
        }

        private static bool IsSpecialChar(char c)
        {
            // where 
            // '|' means "||"
            // '&' means "&&"
            return c == '(' || c == ')' || c == '|' || c == '&';
        }

        private static bool IsOperator(string s, int index, out string @operator)
        {
            @operator = s.Substring(index, 2);
            return @operator == "&&" || @operator == "||";
        }

        private static int OperatorPriority(string o)
        {
            if (o == "&&")
            {
                return 2;
            }
            else if (o == "||")
            {
                return 1;
            }

            return 0; // "(" or ")"
        }
    }
}
