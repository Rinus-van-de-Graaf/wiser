using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Interfaces.DataLayer;
using Api.Modules.VersionControl.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.VersionControl.Services.DataLayer;

public class CommitDataService : ICommitDataService
{
    private readonly IDatabaseConnection databaseConnection;
    
    public CommitDataService(IDatabaseConnection clientDatabaseConnection)
    {
        this.databaseConnection = clientDatabaseConnection;
    }
    
    /// <inheritdoc/>
    public Task<CreateCommitModel> CreateCommitAsync(string commitMessage, string username)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc/>
    public Task CompleteCommitAsync(int commitId, bool commitCompleted)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<List<TemplateCommitModel>> GetTemplatesToCommitAsync()
    {
        var query = $@"SELECT
			template.template_id,
			template.parent_id,
			template.template_name,
			template.version,
			template.changed_on,
			template.published_environment,
			template.template_type,
			IFNULL(test_template.version, 0) AS versie_test,
			IFNULL(acceptatie_template.version, 0) AS versie_acceptatie,
			IFNULL(live_template.version, 0) AS versie_live,
			template.changed_by,
			parent.template_name AS template_parent,
			IF(test_template.version IS NULL, 0, 1) AS test,
			IF(acceptatie_template.version IS NULL, 0, 1) AS accept,
			IF(live_template.version IS NULL, 0, 1) AS live 
		FROM {WiserTableNames.WiserTemplate} AS template
		JOIN {WiserTableNames.WiserTemplate} AS parent ON parent.template_id = template.parent_id AND parent.version = (SELECT MAX(x.version) FROM {WiserTableNames.WiserTemplate} AS x WHERE x.template_id = template.parent_id)
		LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
		LEFT JOIN {WiserTableNames.WiserTemplate} AS test_template ON test_template.template_id = template.template_id AND (test_template.published_environment & 2) = 2
		LEFT JOIN {WiserTableNames.WiserTemplate} AS acceptatie_template ON acceptatie_template.template_id = template.template_id AND (acceptatie_template.published_environment & 4) = 4
		LEFT JOIN {WiserTableNames.WiserTemplate} AS live_template ON live_template.template_id = template.template_id AND (test_template.published_environment & 8) = 8
		LEFT JOIN {WiserTableNames.WiserCommitTemplate} AS templateCommit ON templateCommit.template_id = template.template_id AND templateCommit.version = template.version
		WHERE template.template_type != 7 
		AND template.removed = 0
		AND otherVersion.id IS NULL
		AND templateCommit.id IS NULL
		GROUP BY template.template_id
		ORDER BY template.changed_on ASC";

	    var results = new List<TemplateCommitModel>();
        var dataTable = await databaseConnection.GetAsync(query);
        
	    foreach (DataRow dataRow in dataTable.Rows)
        {
	        var item = new TemplateCommitModel
	        {
		        Environment = (Environments) Convert.ToInt32(dataRow["published_environment"]),
		        Version = dataRow.Field<int>("version"),
		        ChangedBy = dataRow.Field<string>("changed_by"),
		        ChangedOn = dataRow.Field<DateTime>("changed_on"),
		        IsAcceptance = Convert.ToBoolean(dataRow["accept"]),
		        IsLive = Convert.ToBoolean(dataRow["live"]),
		        IsTest = Convert.ToBoolean(dataRow["test"]),
		        TemplateId = dataRow.Field<int>("template_id"),
		        TemplateName = dataRow.Field<string>("template_name"),
		        TemplateType = (TemplateTypes) dataRow.Field<int>("template_type"),
		        VersionAcceptance = dataRow.Field<int>("versie_acceptatie"),
		        VersionLive = dataRow.Field<int>("versie_live"),
		        VersionTest = dataRow.Field<int>("versie_test"),
		        TemplateParentId = dataRow.Field<int>("parent_id"),
		        TemplateParentName = dataRow.Field<string>("template_parent")
	        };
	        results.Add(item);
        }

	    return results;
    }
}