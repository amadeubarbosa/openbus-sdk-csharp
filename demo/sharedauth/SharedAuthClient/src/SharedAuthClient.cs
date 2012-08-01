using System;
using System.Collections.Generic;
using System.IO;
using Ch.Elca.Iiop.Idl;
using omg.org.CORBA;
using omg.org.IOP;
using tecgraf.openbus;
using tecgraf.openbus.core.v2_0.services;
using tecgraf.openbus.core.v2_0.services.access_control;
using tecgraf.openbus.core.v2_0.services.offer_registry;
using tecgraf.openbus.exceptions;
using TypeCode = omg.org.CORBA.TypeCode;

namespace sharedauth {
  internal static class SharedAuthClient {
    private static void Main(String[] args) {
      // Obtém dados através dos argumentos
      string host = args[0];
      ushort port = Convert.ToUInt16(args[1]);
      string helloEntity = args[2];
      string loginFile = args[3];

      // Cria conexão e a define como conexão padrão tanto para entrada como saída.
      // O uso exclusivo da conexão padrão (sem uso de requester e dispatcher) só é recomendado para aplicações que criem apenas uma conexão e desejem utilizá-la em todos os casos. Para situações diferentes, consulte o manual do SDK OpenBus e/ou outras demos.
      IDictionary<string, string> props = new Dictionary<string, string>();
      ConnectionManager manager = ORBInitializer.Manager;
      Connection conn = manager.CreateConnection(host, port, props);
      manager.DefaultConnection = conn;

      // Faz o login usando autenticação compartilhada
      if (!LoginBySharedAuth(loginFile, conn)) {
        Exit(1);
      }

      // Faz busca utilizando propriedades geradas automaticamente e propriedades definidas pelo serviço específico
      // propriedade gerada automaticamente
      ServiceProperty autoProp = new ServiceProperty("openbus.offer.entity",
                                                     helloEntity);
      // propriedade definida pelo serviço hello
      ServiceProperty prop = new ServiceProperty("offer.domain", "OpenBus Demos");
      ServiceProperty[] properties = new[] {prop, autoProp};
      ServiceOfferDesc[] offers = Find(properties, conn);

      // analiza as ofertas encontradas
      Hello hello = GetHello(offers);
      if (hello == null) {
        conn.Logout();
        Exit(1);
      }
      else {
        // utiliza o serviço
        hello.sayHello();
      }

      conn.Logout();
      Console.WriteLine("Fim.");
      Console.ReadLine();
    }

    private static Hello GetHello(ICollection<ServiceOfferDesc> offers) {
      if (offers.Count < 1) {
        Console.WriteLine("O serviço Hello não se encontra no barramento.");
        return null;
      }

      if (offers.Count > 1) {
        Console.WriteLine(
          "Existe mais de um serviço Hello no barramento. Tentaremos encontrar uma funcional.");
      }
      foreach (ServiceOfferDesc serviceOfferDesc in offers) {
        Console.WriteLine("Testando uma das ofertas recebidas...");
        try {
          MarshalByRefObject helloObj =
            serviceOfferDesc.service_ref.getFacet(
              "IDL:Hello:1.0");
          if (helloObj == null) {
            Console.WriteLine(
              "Não foi possível encontrar uma faceta Hello na oferta.");
            continue;
          }
          Hello hello = helloObj as Hello;
          if (hello == null) {
            Console.WriteLine(
              "Faceta encontrada na oferta não implementa Hello.");
            continue;
          }
          Console.WriteLine(
            "Foi encontrada uma oferta com um serviço funcional.");
          return hello;
        }
        catch (TRANSIENT) {
          Console.WriteLine(
            "A oferta é de um cliente inativo. Tentando a próxima.");
        }
      }
      Console.WriteLine(
        "Não foi encontrada uma oferta com um serviço funcional.");
      return null;
    }

    private static ServiceOfferDesc[] Find(ServiceProperty[] properties,
                                           Connection conn) {
      try {
        return conn.Offers.findServices(properties);
      }
      catch (ServiceFailure e) {
        Console.WriteLine(
          "Erro ao tentar realizar a busca por um serviço no barramento: Falha no serviço remoto. Causa:");
        Console.WriteLine(e);
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar a busca por um serviço no barramento:");
        Console.WriteLine(e);
      }
      return null;
    }

    private static bool LoginBySharedAuth(string loginFile, Connection conn) {
      CodecFactory factory =
        OrbServices.GetSingleton().resolve_initial_references("CodecFactory") as
        CodecFactory;
      if (factory == null) {
        Console.WriteLine("Erro ao obter a fábrica de codecs do ORB.");
        return false;
      }
      Codec codec =
        factory.create_codec(new Encoding(ENCODING_CDR_ENCAPS.ConstVal, 1, 2));
      try {
        byte[] encodedLogin = File.ReadAllBytes(loginFile);
        Type saType = typeof (EncodedSharedAuth);
        TypeCode saTypeCode =
          OrbServices.GetSingleton().create_interface_tc(
            Repository.GetRepositoryID(saType), saType.Name);
        EncodedSharedAuth sharedAuth =
          (EncodedSharedAuth) codec.decode_value(encodedLogin, saTypeCode);

        LoginProcess login = sharedAuth.attempt as LoginProcess;
        conn.LoginBySharedAuth(login, sharedAuth.secret);
        return true;
      }
      catch (AlreadyLoggedInException) {
        Console.WriteLine(
          "Falha ao tentar realizar o login por autenticação compartilhada no barramento: a entidade já está com o login realizado. Esta falha será ignorada.");
        return true;
      }
      catch (AccessDenied) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por autenticação compartilhada no barramento: o segredo fornecido não é o esperado.");
      }
      catch (BusChangedException) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por autenticação compartilhada no barramento: o identificador do barramento mudou. Uma nova conexão deve ser criada.");
      }
      catch (InvalidLoginProcessException) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por autenticação compartilhada no barramento: este login por autenticação compartilhada foi cancelado ou expirou. O login deve ser realizado antes que se passe o tempo de lease.");
      }
      catch (ServiceFailure e) {
        Console.WriteLine(
          "Erro ao tentar realizar o login por autenticação compartilhada no barramento: Falha no serviço remoto. Causa:");
        Console.WriteLine(e);
      }
      catch (Exception e) {
        Console.WriteLine(
          "Erro inesperado ao tentar realizar o login por autenticação compartilhada no barramento:");
        Console.WriteLine(e);
      }
      return false;
    }

    private static void Exit(int code) {
      Console.WriteLine("Pressione qualquer tecla para sair.");
      Console.ReadLine();
      Environment.Exit(code);
    }
  }
}