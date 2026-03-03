public enum BattleState
{
    Idle,         // 대기 (전투 시작 전)
    SkillSelect,  // 플레이어가 스킬 선택 중
    ClashResolve, // 클래시 계산 중
    ApplyResult,  // 결과 적용 (데미지, 상태이상)
    BattleEnd     // 전투 종료
}
