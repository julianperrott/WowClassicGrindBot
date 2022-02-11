using System.Collections.Generic;

namespace Core
{
    public static class InfixToPostfix
    {
        public static List<string> Convert(string input)
        {
            List<string> output = new();
            Stack<string> stack = new();

            int i = 0;
            while (i < input.Length)
            {
                if (IsSpecialChar(input[i]))
                {
                    if (input[i] == '(')
                    {
                        stack.Push(input[i].ToString());
                        i++;
                    }
                    else if (input[i] == ')')
                    {
                        while (stack.Count != 0 && stack.Peek() != "(")
                        {
                            output.Add(stack.Pop());
                        }
                        stack.Pop();
                        i++;
                    }
                    else if (IsOperator(input, i, out string @operator))
                    {
                        i += @operator.Length;

                        while (stack.Count != 0 && OperatorPriority(stack.Peek()) >= OperatorPriority(@operator))
                        {
                            output.Add(stack.Pop());
                        }

                        stack.Push(@operator);
                    }
                }
                else
                {
                    int start = i;
                    while (i < input.Length && !IsSpecialChar(input[i]))
                    {
                        i++;
                    }

                    output.Add(input[start..i]); // operand
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
            return c is '(' or ')' or '|' or '&';
        }

        private static bool IsOperator(string s, int index, out string @operator)
        {
            @operator = s.Substring(index, 2);
            return @operator is "&&" or "||";
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
