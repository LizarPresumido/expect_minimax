using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public GameObject Body;
    public PlayerInfo Player;

    private PlayerInfo Target;

    public PlayerList PlayerList;

    public GameState GameState;

    private List<Attack> _attackList;

    public AttackEvent AttackEvent;

    private int maxNodeDepth = 3;

    private Node currentAttack;

    //Clase nodo
    //Funcion de valoración nº puntos de vida + 1/10 * nº puntos de mana // ataques -> media de daño * % acierto
    struct playerData
    {
        public float healthPoints;
        public float manaPoints;

        public playerData(float hp, float mp)
        {
            healthPoints = hp;
            manaPoints = mp;
        }
    }
    class Node
    {
        public static int nodeCount = 0;
        public Node parent;
        public float value;
        bool type;
        public Attack attack;
        public playerData player, enemy;
        bool last;

        public Node() { ++nodeCount; }
        public Node(Node parentNode, Attack nodeAttack, bool type, playerData playerData, playerData enemyData, bool last)
        {
            parent = parentNode;
            attack = nodeAttack;
            value = 0;
            this.type = type;
            player = playerData;
            enemy = enemyData;
            this.last = last;
            ++nodeCount;
            SetValue();
        }
        private void SetValue()
        {
            int counter = 0;
            float damage = 0;
            for (int i = attack.AttackMade.MinDam; i < attack.AttackMade.MaxDam; ++i, ++counter)
                damage += i;
            if (counter != 0)
                damage /= counter;
            else
                damage = 0;

            if (type)
            {
                player.healthPoints -= damage;
                enemy.manaPoints -= attack.AttackMade.Energy;
            }
            else
            {
                enemy.healthPoints -= damage;
                player.manaPoints -= attack.AttackMade.Energy;
            }
            if (last)
            {
                if (player.healthPoints <= 0)
                    value -= 999;
                if (enemy.healthPoints <= 0)
                    value += 999;
                /*if (attack.AttackMade.Name == "Hard" || attack.AttackMade.Name == "Soft")
                    Debug.Log("Player HP: " + player.healthPoints + " / Enemy HP: " + enemy.healthPoints + " / Player MP: " + player.manaPoints + " / Enemy MP: " + enemy.manaPoints);*/
                value = player.healthPoints - enemy.healthPoints + (player.manaPoints - enemy.manaPoints) / 10 + 2 * attack.AttackMade.HitChance;
                /*if(attack.AttackMade.Name == "Hard" || attack.AttackMade.Name == "Soft")
                    Debug.Log(attack.AttackMade.Name +": "+ value);*/
            }
        }
    }

    private void Start()
    {
        Attack _attackToDo;
        currentAttack = null;
        _attackList = new List<Attack>();
        Target = PlayerList.Players.Find(p => p != Player);
        foreach (var playerAttack in Player.Attacks)
        {
            _attackToDo =  ScriptableObject.CreateInstance<Attack>();
            _attackToDo.Source = Player;
            _attackToDo.AttackMade = playerAttack;
            _attackToDo.Target = Target;
            _attackList.Add(_attackToDo);
        }

    }

    public void OnGameTurnChange(PlayerInfo currentTurn)
    {
        if (currentTurn != Player) return;
        Perceive();
        Think();
        Act();
        
    }

    //No uso el percibir por que tiene la lista de los 2 jugadores
    private void Perceive()
    {
        
    }

    private void Think()
    {
        if (currentAttack == null)
            ExpectMiniMax();

    }

    //De manera inicial se podan los ataques para los que no haya energía
    private void ExpectMiniMax()
    {
        currentAttack = getMaxNode(null,1);
    }

    //devulve el nodo con mayor valor para max
    private Node getMaxNode(Node parent, int depth)
    {
        Node bestNode = null;
        Node auxNode = null, auxNode2 = null;
        bool last = (maxNodeDepth == depth);
        int i = 0;
        playerData player = (parent == null) ? new playerData (Player.HP,Player.Energy) : parent.player;
        playerData enemy = (parent == null) ? new playerData(Target.HP, Target.Energy) : parent.enemy;
        for (i = 0; i < _attackList.Count && _attackList[i].AttackMade.Energy > player.manaPoints; ++i)
            ;
        auxNode = new Node(parent, _attackList[i], false, player, enemy, last);
        bestNode = auxNode;
        if (!last)
        {
            for (; i < _attackList.Count; ++i)
            {
                //Debug.Log(_attackList[i].AttackMade.Name);
                if (_attackList[i].AttackMade.Energy <= player.manaPoints)
                {
                    auxNode = new Node(parent, _attackList[i], false, player, enemy, last);
                    auxNode2 = getMinNode(auxNode, depth + 1);
                    //Debug.Log(bestNode.value +" / "+ auxNode2.value);
                    if (bestNode.value < auxNode2.value)
                    {
                        //Debug.Log("CAMBIOOOOOOOOOOOOOO");
                        bestNode = auxNode;
                        bestNode.value = auxNode2.value;
                    }
                }
            }
        }
        else
        {
            for (; i < _attackList.Count; ++i)
            {
                if (_attackList[i].AttackMade.Energy <= player.manaPoints)
                {
                    //Debug.Log(_attackList[i].AttackMade.Name);
                    auxNode = new Node(parent, _attackList[i], true, parent.player, parent.enemy, last);
                    //Debug.Log(_attackList[i].AttackMade.Name);
                    if (bestNode.value < auxNode.value)
                        bestNode = auxNode;
                }
            }
        }
        return bestNode;

    }

    //devuelve el nodo con mayor valor para min
    private Node getMinNode(Node parent, int depth)
    {
        Node bestNode = null;
        Node auxNode = null, auxNode2 = null;
        int i = 0;
        for (i = 0; i < _attackList.Count && _attackList[i].AttackMade.Energy > parent.enemy.manaPoints; ++i)
            ;
        auxNode = new Node(parent, _attackList[i], false, parent.player, parent.enemy, false);
        auxNode2 = getMaxNode(auxNode, depth + 1);
        bestNode = auxNode;
        bestNode.value = auxNode2.value;
        for (; i < _attackList.Count; ++i)
        {
            if (_attackList[i].AttackMade.Energy <= parent.enemy.manaPoints)
            {
                //Debug.Log(_attackList[i].AttackMade.Name);
                auxNode = new Node(parent, _attackList[i], false, parent.player, parent.enemy, false);
                auxNode2 = getMaxNode(auxNode, depth + 1);
                //Debug.Log(bestNode.value + " / " + auxNode2.value);
                if (bestNode.value < auxNode2.value)
                {
                    bestNode = auxNode;
                    bestNode.value = auxNode2.value;
                }
            }
        }
        return bestNode;
    }

    //Actúa con el ataque que el algoritmo minimax haya considerado más óptimo
    private void Act()
    {
        AttackEvent.Raise(currentAttack.attack);
        currentAttack = null;
        //Debug.Log(Node.nodeCount);
    }


}
