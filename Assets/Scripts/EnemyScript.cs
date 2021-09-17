using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    [SerializeField] private int _maxHealth = 1;
    [SerializeField] private float _moveSpeed = 1f;
    [SerializeField] private SpriteRenderer _healthBar;
    [SerializeField] private SpriteRenderer _healthFill;

    public Vector3 TargetPosition { get; set; }
    public int CurrentPathIndex { get; set; }

    private bool isLast;
    private int _currentHealth;

    // Fungsi ini terpanggil sekali setiap kali menghidupkan game object yang memiliki script ini
    private void OnEnable()
    {
        CurrentPathIndex = 0;
        _healthFill.transform.localScale = Vector3.one;
        _currentHealth = _maxHealth;
        _healthFill.size = _healthBar.size;
    }

    public void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, TargetPosition, _moveSpeed * Time.deltaTime);
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        TargetPosition = targetPosition;
        _healthBar.transform.parent = null;

        // Mengubah rotasi dari enemy
        Vector3 distance = TargetPosition - transform.position;
        if (Mathf.Abs(distance.y) > Mathf.Abs(distance.x))
        {
            // Menghadap atas
            if (distance.y > 0)
            {
                transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 90f));
            }
            // Menghadap bawah
            else
            {
                transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, -90f));
            }
        }
        else
        {
            // Menghadap kanan (default)
            if (distance.x > 0)
            {
                transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
            }
            // Menghadap kiri
            else
            {
                transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 180f));
            }
        }

        _healthBar.transform.parent = transform;
    }

    public void ReduceEnemyHealth(int damage)
    {
        AudioScript.Instance.PlaySFX("hit-enemy");
        _currentHealth -= damage;

        //-Health Bar Value
        Vector3 scale = _healthFill.transform.localScale;
        _healthFill.transform.localScale =
            new Vector3(scale.x - damage / (_healthBar.transform.localScale.x * _maxHealth),
                        scale.y, scale.z);

        if (_currentHealth <= 0)
        {
            AudioScript.Instance.PlaySFX("enemy-die");
            gameObject.SetActive(false);
        }
    }

    //Chek If Last Enemy
    public void SetLast(bool b)
    {
        isLast = b;
    }
    // Menandai indeks terakhir pada path
    public void SetCurrentPathIndex(int currentIndex)
    {
        CurrentPathIndex = currentIndex;
    }

    private void OnDisable()
    {
        if (isLast) FindObjectOfType<LevelManagerScript>().LastEnemyKilled();
    }
}