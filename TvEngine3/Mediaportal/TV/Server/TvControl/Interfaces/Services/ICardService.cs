using System.Collections.Generic;
using System.ServiceModel;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVDatabase.Entities.Enums;

namespace Mediaportal.TV.Server.TVControl.Interfaces.Services
{
  [ServiceContract(Namespace = "http://www.team-mediaportal.com")]
  public interface ICardService
  {
    [OperationContract]
    IList<Card> ListAllCards();

    [OperationContract(Name = "ListAllCardsWithSpecificRelations")]
    IList<Card> ListAllCards(CardIncludeRelationEnum includeRelations);

    [OperationContract]
    IList<Card> SaveCards(IEnumerable<Card> cards);

    [OperationContract]
    Card GetCardByDevicePath(string cardDevice);

    [OperationContract(Name = "GetCardByDevicePathWithSpecificRelations")]
    Card GetCardByDevicePath(string cardDevice, CardIncludeRelationEnum includeRelations);

    [OperationContract]
    Card SaveCard(Card card);

    [OperationContract]
    void DeleteCard(int idCard);

    [OperationContract]
    DisEqcMotor SaveDisEqcMotor(DisEqcMotor motor);

    [OperationContract]
    Card GetCard(int idCard);

    [OperationContract(Name = "GetCardWithSpecificRelations")]
    Card GetCard(int idCard, CardIncludeRelationEnum includeRelations);

    [OperationContract]
    CardGroup SaveCardGroup(CardGroup @group);

    [OperationContract]
    void DeleteCardGroup(int idCardGroup);

    [OperationContract]
    IList<CardGroup> ListAllCardGroups();

    [OperationContract]
    IList<SoftwareEncoder> ListAllSofwareEncodersVideo();

    [OperationContract]
    IList<SoftwareEncoder> ListAllSofwareEncodersAudio();

    [OperationContract]
    IList<Satellite> ListAllSatellites();

    [OperationContract]
    Satellite SaveSatellite(Satellite satellite);

    [OperationContract]
    SoftwareEncoder SaveSoftwareEncoder(SoftwareEncoder encoder);

    [OperationContract]
    void DeleteGroupMap(int idMap);

    [OperationContract]
    CardGroupMap SaveCardGroupMap(CardGroupMap map);

    [OperationContract]
    LnbType GetLnbType(int idLnbType);

    [OperationContract]
    IList<LnbType> ListAllLnbTypes();
  }
}