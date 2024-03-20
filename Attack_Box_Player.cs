using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack_Box_Player :MonoBehaviour
{
    [HideInInspector] public List<Enemy> enemies = new List<Enemy>();
    [HideInInspector] public int skillIndex;
    public Player player;

    private void OnEnable() => enemies.Clear();

    void Start()
    {
        player = transform.parent.GetComponent<Player>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy") && !enemies.Contains(collision.GetComponent<Enemy>()))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            enemies.Add(enemy);
            float damage = skillIndex == 1 ? player.Damage_Skill1 : 
                           skillIndex == 2 ? player.Damage_Skill2 : 
                                             player.Damage;
            enemy.Damaged(damage);
        }
    }
}
