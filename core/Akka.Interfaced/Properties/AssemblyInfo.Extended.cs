using System.Runtime.CompilerServices;
using System.Security;

[assembly: InternalsVisibleTo("Akka.Interfaced-Persistence")]

//// http://stackoverflow.com/questions/5053032/performance-of-compiled-to-delegate-expression
//[assembly: AllowPartiallyTrustedCallers]
//[assembly: SecurityTransparent]
//[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]
