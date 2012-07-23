// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

using System;
using System.Text.RegularExpressions;

namespace Conference.Specflow.Support
{
    public class Slug
    {
        private static readonly Regex validation = new Regex("[A-Z0-9]{6}", RegexOptions.Compiled);

        public Slug(string value)
        {
            Value = value;
        }

        public string Value { get; set; }

        public static Slug CreateNew()
        {
            return new Slug(TestHandleGenerator.Generate(10));
        }

        public static Regex FindBy
        {
            get { return validation; }
        }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Value) &&
                        validation.IsMatch(Value);
            }
        }

        static class TestHandleGenerator
        {
            private static readonly Random rnd = new Random();
            private static readonly char[] allowableChars = "ABCDEFGHJKMNPQRSTUVWXYZ123456789".ToCharArray();

            public static string Generate(int length)
            {
                var result = new char[length];
                lock (rnd)
                {
                    for (int i = 0; i < length; i++)
                    {
                        result[i] = allowableChars[rnd.Next(0, allowableChars.Length)];
                    }
                }

                return new string(result);
            }
        }
    }
    
}
