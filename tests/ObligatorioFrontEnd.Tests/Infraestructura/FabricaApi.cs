using ObligatorioFrontEnd.Api;
using ObligatorioFrontEnd.Services;

namespace ObligatorioFrontEnd.Tests.Infraestructura;

public sealed record EntornoApi(
    ClienteApi Api,
    HttpClient Http,
    TokenStore Token,
    CacheDatos Cache,
    ManejadorFalso Manejador);

public static class FabricaApi
{
    public const string BaseUrl = "http://api.test/";

    public static EntornoApi Crear(Func<SolicitudRegistrada, HttpResponseMessage> responder) =>
        Crear(new ManejadorFalso(responder));

    public static EntornoApi Crear(Func<SolicitudRegistrada, int, HttpResponseMessage> responder) =>
        Crear(new ManejadorFalso(responder));

    public static EntornoApi Crear(ManejadorFalso manejador)
    {
        var http = new HttpClient(manejador)
        {
            BaseAddress = new Uri(BaseUrl)
        };

        var token = new TokenStore();
        var cache = new CacheDatos();
        var api = new ClienteApi(http, token, cache);

        return new EntornoApi(api, http, token, cache, manejador);
    }
}
