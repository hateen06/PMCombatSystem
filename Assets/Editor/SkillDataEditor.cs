using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SkillData))]
public class SkillDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var skill = (SkillData)target;

        // 제목
        EditorGUILayout.LabelField("스킬 정보", EditorStyles.boldLabel);
        skill.skillName = EditorGUILayout.TextField("스킬 이름", skill.skillName);
        skill.skillType = (SkillType)EditorGUILayout.EnumPopup("타입", skill.skillType);

        EditorGUILayout.Space(10);

        // 위력 섹션
        EditorGUILayout.LabelField("위력 설정", EditorStyles.boldLabel);
        skill.basePower = EditorGUILayout.IntSlider("기본 위력", skill.basePower, 1, 20);
        skill.coinCount = EditorGUILayout.IntSlider("코인 수", skill.coinCount, 1, 5);
        skill.coinPower = EditorGUILayout.IntSlider("코인당 위력", skill.coinPower, 1, 10);

        // 예상 위력 미리보기
        int minPower = skill.basePower;
        int maxPower = skill.basePower + skill.coinCount * skill.coinPower;
        EditorGUILayout.HelpBox(
            $"예상 위력 범위: {minPower} ~ {maxPower}\n" +
            $"평균 위력: {(minPower + maxPower) / 2f:F1}",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 상태이상 섹션
        EditorGUILayout.LabelField("상태이상 부여", EditorStyles.boldLabel);
        skill.inflictStatus = (StatusType)EditorGUILayout.EnumPopup("상태이상", skill.inflictStatus);
        skill.statusPotency = EditorGUILayout.IntSlider("위력", skill.statusPotency, 0, 10);
        skill.statusCount = EditorGUILayout.IntSlider("횟수", skill.statusCount, 0, 5);

        if (skill.statusPotency > 0 && skill.statusCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"{skill.inflictStatus}: 위력 {skill.statusPotency} x {skill.statusCount}회",
                MessageType.Warning);
        }

        // 변경 감지
        if (GUI.changed)
            EditorUtility.SetDirty(skill);
    }
}
