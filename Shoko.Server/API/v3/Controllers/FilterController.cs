using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shoko.Models.Enums;
using Shoko.Server.API.Annotations;
using Shoko.Server.API.v3.Helpers;
using Shoko.Server.API.v3.Models.Shoko;
using Shoko.Server.Models;
using Shoko.Server.Repositories;

namespace Shoko.Server.API.v3.Controllers
{
    [ApiController, Route("/api/v{version:apiVersion}/[controller]"), ApiV3]
    [Authorize]
    public class FilterController : BaseController
    {
        internal static string FilterNotFound = "No Filter entry for the given filterID";

        /// <summary>
        /// Get All <see cref="Filter"/>s
        /// </summary>
        /// <param name="includeEmpty"></param>
        /// <param name="includeInvisible"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<List<Filter>> GetAllFilters([FromQuery] bool includeEmpty = false, [FromQuery] bool includeInvisible = false, [FromQuery] int page = 0, [FromQuery] int pageSize = 10)
        {
            var groupFilters = RepoFactory.GroupFilter.GetTopLevel()
                .Where(filter =>
                {
                    if (filter.InvisibleInClients != 0 && !includeInvisible)
                        return false;
                    if (filter.GroupsIds.ContainsKey(User.JMMUserID) && filter.GroupsIds[User.JMMUserID].Count > 0 || includeEmpty)
                        return true;
                    return ((GroupFilterType)filter.FilterType).HasFlag(GroupFilterType.Directory);
                })
                .OrderBy(filter => filter.GroupFilterName);

            if (pageSize <= 0)
                return groupFilters
                    .Select(filter => new Filter(HttpContext, filter))
                    .ToList();

            if (page <= 0) page = 0;
            return groupFilters
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(filter => new Filter(HttpContext, filter))
                .ToList();
        }

        /// <summary>
        /// Create or update a filter
        /// </summary>
        /// <param name="body"></param>
        /// <returns>The resulting Filter, with ID</returns>
        [HttpPost]
        public ActionResult<Filter> SaveFilter(Filter.FullFilter body)
        {
            SVR_GroupFilter groupFilter = null;
            if (body.IDs.ID != 0)
            {
                groupFilter = RepoFactory.GroupFilter.GetByID(body.IDs.ID);
                if (groupFilter == null)
                    return NotFound(FilterNotFound);
                if (groupFilter.Locked == 1)
                    return Forbid("Filter is Locked");
            }
            groupFilter = body.ToServerModel(groupFilter);
            groupFilter.CalculateGroupsAndSeries();
            RepoFactory.GroupFilter.Save(groupFilter);

            return new Filter(HttpContext, groupFilter);
        }

        /// <summary>
        /// Preview the Groups that will be in the filter if the changes are applied
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        [HttpPost("Preview")]
        public ActionResult<List<Group>> PreviewFilterChanges(Filter.FullFilter body)
        {
            var groupFilter = body.ToServerModel();
            groupFilter.CalculateGroupsAndSeries();

            if (!groupFilter.GroupsIds.TryGetValue(User.JMMUserID, out var groupIDs))
                return new List<Group>();

            return groupIDs
                .Select(a => RepoFactory.AnimeGroup.GetByID(a))
                .Where(a => a != null)
                .GroupFilterSort(groupFilter)
                .Select(a => new Group(HttpContext, a))
                .ToList();
        }

        /// <summary>
        /// Get the <see cref="Filter"/> for the given <paramref name="filterID"/>.
        /// </summary>
        /// <param name="filterID">Filter ID</param>
        /// <returns></returns>
        [HttpGet("{filterID}")]
        public ActionResult<Filter> GetFilter(int filterID)
        {
            var groupFilter = RepoFactory.GroupFilter.GetByID(filterID);
            if (groupFilter == null)
                return NotFound(FilterNotFound);

            return new Filter(HttpContext, groupFilter);
        }

        /// <summary>
        /// Delete a filter
        /// </summary>
        /// <param name="filterID"></param>
        /// <returns></returns>
        [Authorize("admin")]
        [HttpDelete("{filterID}")]
        public ActionResult DeleteFilter(int filterID)
        {
            var groupFilter = RepoFactory.GroupFilter.GetByID(filterID);
            if (groupFilter == null)
                return NotFound(FilterNotFound);

            RepoFactory.GroupFilter.Delete(groupFilter);
            return NoContent();
        }

        /// <summary>
        /// Get Conditions for Filter with id
        /// </summary>
        /// <param name="filterID"></param>
        /// <returns></returns>
        [HttpGet("{filterID}/Conditions")]
        public ActionResult<Filter.FilterConditions> GetFilterConditions(int filterID)
        {
            var groupFilter = RepoFactory.GroupFilter.GetByID(filterID);
            if (groupFilter == null)
                return NotFound(FilterNotFound);

            return Filter.GetConditions(groupFilter);
        }

        /// <summary>
        /// Get Sorting Criteria for Filter with id
        /// </summary>
        /// <param name="filterID"></param>
        /// <returns></returns>
        [HttpGet("{filterID}/Sorting")]
        public ActionResult<List<Filter.SortingCriteria>> GetFilterSortingCriteria(int filterID)
        {
            var groupFilter = RepoFactory.GroupFilter.GetByID(filterID);
            if (groupFilter == null)
                return NotFound(FilterNotFound);

            return Filter.GetSortingCriteria(groupFilter);
        }
    }
}