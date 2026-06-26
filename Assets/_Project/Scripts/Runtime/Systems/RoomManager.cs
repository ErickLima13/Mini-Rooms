using UnityEditor;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [Header("atributos maximos")]
    public int vidaMax;
    public int enemiesMax;
    public int tesouroMax;

    [Header("atributos atuais")]
    public int vidaAtual;
    public int enemiesAtual;
    public int tesouroAtual;

    [Header("sala valores")]
    public int numInimigos;
    public int numTesouros;
    public int numVida;

    private void Start()
    {
        CriaSala();
    }

    public void CriaSala()
    {
        if (ChecaValorMenor(vidaAtual, vidaMax)) // tomou dano na sala ?
        {
            numInimigos++;
        }
        else
        {
            numInimigos--;
        }

        if (ChecaValorIgual(enemiesAtual, enemiesMax))//  matou todos inimigos da sala ?
        {
            numTesouros++;
        }
        else
        {
            numTesouros--;
        }

        if (ChecaValorIgual(tesouroAtual, tesouroMax)) // pegou todos tesouros da sala ?
        {
            numVida++;
        }
        else
        {
            numVida--;
        }

        numInimigos = Mathf.Max(0, numInimigos);
        numTesouros = Mathf.Max(0, numTesouros);
        numVida = Mathf.Max(0, numVida);
    }

    // tenho que checar se o valor atual é menor que o valor max, e somar ou subtrair os valores da sala
    // mas no caso de inimigos/tesouro checa o contrario

    private bool ChecaValorMenor(int valor1, int valor2)
    {
        return valor1 < valor2;
    }

    private bool ChecaValorIgual(int valor1, int valor2)
    {
        return valor1 == valor2;
    }

    public void ZeraSala()
    {
        numInimigos = 0;
        numVida = 0;
        numTesouros = 0;

        vidaAtual = 0;
        tesouroAtual = 0;
        enemiesAtual = 0;
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(RoomManager))]
public class RoomManagerControl : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space(10f);
        RoomManager controller = (RoomManager)target;
        if (GUILayout.Button("Criar sala"))
        {
            controller.CriaSala();
        }

        if (GUILayout.Button("Zera Sala"))
        {
            controller.ZeraSala();
        }
    }
}
#endif