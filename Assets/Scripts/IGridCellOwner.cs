/// <summary>
/// GridCell의 클릭 이벤트를 처리하는 보드 매니저 인터페이스.
/// BoardManager와 RotatingBoardManager 모두 구현함.
/// </summary>
public interface IGridCellOwner
{
    void OnCellClicked(GridCell cell);
}
