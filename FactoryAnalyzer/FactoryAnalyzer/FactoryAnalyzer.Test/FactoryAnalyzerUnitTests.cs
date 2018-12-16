using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace FactoryAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestMethod2()
        {
            var test = @"
                        using System;

                        namespace ConsoleApplication1
                        {
                            public class Test 
                            {
                                public static void M()
                                {
                                    F(new Derived1());
                                }

                                static void F(Derived d)
                                {
                                }
                            }

                            

                            public class Base
                            {
                                public static T Create<T>() where T : Base, new()
                                {
                                    return new T();
                                }
                            }
                            public class Derived : Base
                            {   
                            }
                            public class Derived1 : Derived
                            {   
                            }
                        }";

            var fixedText = @"
                        using System;

                        namespace ConsoleApplication1
                        {
                            public class Test 
                            {
                                public static void M()
                                {
                                    F(Base.Create<Derived1>());
                                }

                                static void F(Derived d)
                                {
                                }
                            }

                            

                            public class Base
                            {
                                public static T Create<T>() where T : Base, new()
                                {
                                    return new T();
                                }
                            }
                            public class Derived : Base
                            {   
                            }
                            public class Derived1 : Derived
                            {   
                            }
                        }";

            var expected = new DiagnosticResult
            {
                Id = FactoryAnalyzer.DiagnosticId,
                Message = $"This class should be created by factory method Create of class Base",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 39) }
            };

            VerifyCSharpDiagnostic(test, expected);

            VerifyCSharpFix(test, fixedText);
        }

    }
}
