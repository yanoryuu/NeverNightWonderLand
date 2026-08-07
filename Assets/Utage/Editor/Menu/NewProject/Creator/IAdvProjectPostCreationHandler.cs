namespace Utage
{
	//「UTAGE」のプロジェクト作成後の処理のためのインターフェース
	public interface IAdvProjectPostCreationHandler
	{
		//プロジェクト作成後の処理
		void OnPostCreateProject(AdvProjectCreator project);
	}
}
