using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum EnemyColor { Azul, Naranja, Verde, Amarillo }

    [Header("Enemy")]
    public EnemyColor enemyColor = EnemyColor.Azul;
    public GameObject destroyVFX;

    private void Start()
    {
        // Asigna un color aleatorio al enemigo al instanciarlo
        GetRandomColor();
    }

    public void Hit(PlayerChoices.PlayerColor shooter)
    {
        // +1 si el color NO coincide, -1 si coincide
        if(Map(shooter) == enemyColor) 
            Debug.Log("MISMO COLOR!");
        else 
            Debug.Log("COLOR DISTINTO!");

        //Minigame_2.Instance.AddScore(shooter, delta);

        if (destroyVFX) Instantiate(destroyVFX, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private EnemyColor Map(PlayerChoices.PlayerColor pc)
    {
        return pc switch
        {
            PlayerChoices.PlayerColor.Azul => EnemyColor.Azul,
            PlayerChoices.PlayerColor.Naranja => EnemyColor.Naranja,
            PlayerChoices.PlayerColor.Verde => EnemyColor.Verde,
            PlayerChoices.PlayerColor.Amarillo => EnemyColor.Amarillo,
            _ => EnemyColor.Azul
        };
    }

    private void GetRandomColor()
    {
        int rand = Random.Range(0, 4);
        enemyColor = (EnemyColor)rand;

        switch(enemyColor)
        {
            case EnemyColor.Azul:
                GetComponentInChildren<Renderer>().SetMaterials(new List<Material>() { PlayerChoices.Instance.colorMaterials[0] });
                break;
            case EnemyColor.Naranja:
                GetComponentInChildren<Renderer>().SetMaterials(new List<Material>() { PlayerChoices.Instance.colorMaterials[1] });
                break;
            case EnemyColor.Verde:
                GetComponentInChildren<Renderer>().SetMaterials(new List<Material>() { PlayerChoices.Instance.colorMaterials[2] });
                break;
            case EnemyColor.Amarillo:
                GetComponentInChildren<Renderer>().SetMaterials(new List<Material>() { PlayerChoices.Instance.colorMaterials[3] });
                break;
        }
    }
}
