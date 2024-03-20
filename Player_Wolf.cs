using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Wolf : Player
{
    protected override void Init()
    {
        stpd.skul = PlayerSkul.Wolf;
        Damage = 15;
        Damage_Skill1 = 40;

        base.Init();
        
        StartCoroutine(Switched());
        switchIndex = 0;
    }

    #region 스킬1
    protected override IEnumerator CSkill_1()
    {
        rigid.velocity = new Vector2(0, rigid.velocity.y);
        canSkill_1 = false;
        animator.SetTrigger("Skill_1");
        StartCoroutine(CCoolDown_UI(ProjectManager.Instance.ui.skill1_Mask, 3));
        yield return new WaitForSeconds(3f);
        canSkill_1 = true;
    }

    #endregion

    #region 스킬2
    protected override IEnumerator CSkill_2()
    {
        yield break;
    }

    #endregion

    #region 교대
    //교대 스킬 발동
    protected override void SwitchSkill()
    {
        LookDir();
        animator.SetTrigger("Switch");
        isSwitched = false;
    }

    //교대 시작하자마자 event
    void EventSwitchAnimation()
    {
        Teleport_Attack(10);
    }

    void Teleport_Attack(float distance)
    {
        float tempDis;
        int layer = 1 << LayerMask.NameToLayer("Ground");
        RaycastHit2D hit1 = Physics2D.Raycast(gameObject.transform.position, transform.right, distance, layer);
        RaycastHit2D hit2 = Physics2D.Raycast(gameObject.transform.position + (playerCol.size.y) * transform.up, transform.right, distance, layer);

        if (!hit1 && !hit2)
        {
            //transform.Translate(distance * Vector2.right);
            tempDis = distance;
        }
        else if (hit1 == hit2)
        {
            //transform.Translate((hit1.distance - capCol.size.x / 2) * Vector2.right);
            tempDis = hit1.distance - playerCol.size.x / 2;
        }
        else
        {
            tempDis = hit1 ? hit1.distance : hit2.distance;
            if (hit1 && hit2)
                tempDis = hit1.distance < hit2.distance ? hit1.distance : hit2.distance;
        }

        int layer2 = 1 << LayerMask.NameToLayer("Enemy");
        RaycastHit2D[] hit3 = Physics2D.RaycastAll(gameObject.transform.position + (playerCol.size.y / 2) * transform.up, transform.right, tempDis, layer2);

        foreach (var item in hit3)
        {
            if (item.collider.gameObject.GetComponent<Enemy>())
            {
                Enemy enemy = item.collider.gameObject.GetComponent<Enemy>();
                SetDamage(enemy, 40);
            }
        }

        tempDis -= playerCol.size.x / 2;
        transform.Translate(tempDis * Vector2.right);
    }
    #endregion
}
