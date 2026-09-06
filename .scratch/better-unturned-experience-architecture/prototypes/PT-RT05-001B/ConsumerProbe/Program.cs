using PtRt05.ConnectionTokens;

var contextType = typeof(ConnectionReceiveContext);
var publicConstructors = contextType.GetConstructors();
var publicDeclaredMethods = contextType.GetMethods()
    .Where(method => method.DeclaringType == contextType)
    .Select(method => method.Name)
    .ToArray();
var mutations = 0;
var nullAccepted = ConnectionBoundDispatch.TryInvoke(null, () => mutations++);

if (publicConstructors.Length != 0 ||
    publicDeclaredMethods.Contains("Revoke", StringComparer.Ordinal) ||
    nullAccepted ||
    mutations != 0)
{
    Console.WriteLine("FAIL | independent consumer capability surface");
    return 1;
}

Console.WriteLine("PASS | independent consumer cannot construct/revoke; null rejects fail-closed");
return 0;
