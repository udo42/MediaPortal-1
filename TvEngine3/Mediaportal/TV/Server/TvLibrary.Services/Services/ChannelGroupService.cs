using System.Collections.Generic;
using Mediaportal.TV.Server.TVControl.Interfaces.Services;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVDatabase.Entities.Enums;
using Mediaportal.TV.Server.TVDatabase.TVBusinessLayer;

namespace Mediaportal.TV.Server.TVLibrary.Services
{  
  public class ChannelGroupService : IChannelGroupService
  {
    #region IChannelGroupService Members

    public IList<ChannelGroup> ListAllChannelGroups()
    {
      IList<ChannelGroup> listAllChannelGroups = ChannelGroupManagement.ListAllChannelGroups();
      return listAllChannelGroups;
    }

    public IList<ChannelGroup> ListAllChannelGroups(ChannelGroupIncludeRelationEnum includeRelations)
    {
      IList<ChannelGroup> listAllChannelGroups = ChannelGroupManagement.ListAllChannelGroups(includeRelations);
      return listAllChannelGroups;
    }

    public IList<ChannelGroup> ListAllChannelGroupsByMediaType(MediaTypeEnum mediaTypeEnum, ChannelGroupIncludeRelationEnum includeRelations)
    {
      IList<ChannelGroup> listAllChannelGroupsByMediaType = ChannelGroupManagement.ListAllChannelGroupsByMediaType(mediaTypeEnum, includeRelations);
      return listAllChannelGroupsByMediaType;
    }

    public IList<ChannelGroup> ListAllCustomChannelGroups(ChannelGroupIncludeRelationEnum includeRelations, MediaTypeEnum mediaType)
    {
      IList<ChannelGroup> listAllCustomChannelGroups = ChannelGroupManagement.ListAllCustomChannelGroups(includeRelations);
      return listAllCustomChannelGroups;
    }

    public IList<ChannelGroup> ListAllChannelGroupsByMediaType(MediaTypeEnum mediaType)
    {
      IList<ChannelGroup> listAllChannelGroupsByMediaType = ChannelGroupManagement.ListAllChannelGroupsByMediaType(mediaType);
      return listAllChannelGroupsByMediaType;
    }

    public ChannelGroup GetChannelGroupByNameAndMediaType(string groupName, MediaTypeEnum mediaType)
    {
      ChannelGroup channelGroupByNameAndMediaType = ChannelGroupManagement.GetChannelGroupByNameAndMediaType(groupName, mediaType);
      return channelGroupByNameAndMediaType;
    }
       
    public void DeleteChannelGroupMap(int idMap)
    {
      ChannelGroupManagement.DeleteChannelGroupMap(idMap);
    }

    public ChannelGroup GetChannelGroup(int id)
    {
      return ChannelGroupManagement.GetChannelGroup(id);
    }

    public ChannelGroup SaveGroup(ChannelGroup group)
    {
      return ChannelGroupManagement.SaveGroup(group);
    }

    public void DeleteChannelGroup(int idGroup)
    {
      ChannelGroupManagement.DeleteChannelGroup(idGroup);
    }

    public ChannelGroup GetOrCreateGroup(string groupName, MediaTypeEnum mediaType)
    {
      ChannelGroup orCreateGroup = ChannelGroupManagement.GetOrCreateGroup(groupName, mediaType);
      return orCreateGroup;
    }

    #endregion
  }
}
