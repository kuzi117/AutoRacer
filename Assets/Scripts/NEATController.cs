using System;
using System.Collections;
using System.Collections.Generic;
using SharpNeat.Phenomes;
using UnityEngine;
using UnitySharpNEAT;
using Object = System.Object;

public class NEATController : UnitController
{
  private SpriteRenderer _sprite;
  private int _mask;
  private Vector3 _spawnPos;
  private Quaternion _spawnRot;

  private int _collisions;
  private int _progress;
  private int _lap;
  private int _sections;

  public bool usePhysics;
  public float maxSpeed;
  public float impulse;
  public float turnSpeed;
  public Sprite[] carSprites;
  public GameObject castOrigin;
  
  private void UnitReset() {
    transform.position = _spawnPos;
    transform.rotation = _spawnRot;
    
    GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    GetComponent<Rigidbody2D>().angularVelocity = 0;

    _progress = 0;
    _lap = 0;
    _collisions = 0;
  }
  
  void Start() {
    _sprite = GetComponent<SpriteRenderer>();
    _sprite.sprite = carSprites[0];
    
    _mask = LayerMask.GetMask("Barriers");
    
    GameObject spawn = GameObject.FindGameObjectWithTag("Respawn"); 
    _spawnPos = spawn.transform.position;
    _spawnRot = spawn.transform.rotation;

    _sections = GameObject.Find("Track").GetComponent<ProgressTotal>().sections;

    UnitReset();
  }

  protected override void UpdateBlackBoxInputs(ISignalArray inputSignalArray) {
    for (int i = 0; i < 5; ++i) {
      Vector3 origin = castOrigin.transform.position;
      Vector3 dir = Quaternion.Euler(0, 0, -50 + i * 25) * transform.up;
      Debug.DrawRay(origin, dir, Color.magenta);
      RaycastHit2D hit = Physics2D.Raycast(origin, dir, 1f, _mask);
      if (hit.collider != null) {
        inputSignalArray[i] = hit.distance;
      }
    }
  }

  protected override void UseBlackBoxOutputs(ISignalArray outputSignalArray) {
    bool forward = outputSignalArray[0] > 0.5;
    bool left = outputSignalArray[1] > 0.5;
    bool right = outputSignalArray[2] > 0.5;

    if (forward) {
      if (left ^ right) {
        int sign = left ? 1 : -1;
        this.transform.Rotate(0, 0, turnSpeed * sign);
        _sprite.sprite = left ? carSprites[1] : carSprites[2];
      }
      
      if (!usePhysics)
        this.transform.Translate(Vector3.up * maxSpeed);
      else {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body.velocity.magnitude < maxSpeed)
          body.AddForce(transform.up * impulse);
      }
    }
  }

  public override float GetFitness() {
    float progress = _lap + (_progress / (float) _sections);
    float maxProgressLoss = progress / 2;
    float progressLoss = Math.Min(_collisions * (1 / (3f * _sections)), maxProgressLoss);
    return progress - progressLoss;
  }

  protected override void HandleIsActiveChanged(bool newIsActive) {
    if (!newIsActive) {
      UnitReset();
    }
  }

  private void OnCollisionEnter2D(Collision2D other) {
    if (other.gameObject.CompareTag("Wall")) {
      ++_collisions;
    }
  }

  private void OnCollisionStay2D(Collision2D other) {
    if (other.gameObject.CompareTag("Wall")) {
      ++_collisions;
    }
  }

  private void OnTriggerEnter2D(Collider2D other) {
    ProgressMarker progress = other.GetComponent<ProgressMarker>();
    if (progress.progress == _progress + 1) {
      if (progress.isLast) {
        _progress = 0;
        ++_lap;
      }
      else {
        _progress = progress.progress;
      }
    }
  }
}
