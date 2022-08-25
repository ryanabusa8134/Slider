using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class Meltable : MonoBehaviour, ISavable
{
    [Header("Sprites")]
   // [SerializeField] private Animator animator; C: Will switch over later
    
    [SerializeField] private Sprite frozenSprite;
    [SerializeField] private Sprite meltedSprite;
    [SerializeField] private Sprite anchorBrokenSprite;
    [SerializeField] private SpriteRenderer spriteRenderer;


    [Header("Events")]
    public UnityEvent onMelt;
    public UnityEvent onFreeze;
    public UnityEvent onBreak;
    public UnityEvent onFix;


    [Header("Properties")]
    [SerializeField] private bool canBreakWithAnchor = true;
    [SerializeField] private bool canBreakWhenNotFrozen = false;
    [SerializeField] private bool refreezeOnTop = true;
    [SerializeField] private bool refreezeFromBroken = false;
    [SerializeField] private bool fixBackToFrozen = false;

    public bool isFrozen = true;
    public int numLavaSources = 0;
    private bool anchorBroken = false;
    private int numTimesBroken = 0;
    private STile sTile;


    private void OnEnable() {
        sTile = GetComponentInParent<MountainSTile>();
        SGridAnimator.OnSTileMoveEndEarly += CheckFreezeOnMoveEnd; //C: Has to be early + delay or else tile in args is null
    }

    private void OnDisable() {
        SGridAnimator.OnSTileMoveEndEarly -= CheckFreezeOnMoveEnd;
    }

    public void CheckFreezeOnMoveEnd(object sender, SGridAnimator.OnTileMoveArgs e)
    {
        if(e.stile == null)
            return;
        if(e.stile == sTile)
            StartCoroutine(WaitThenCheckFreeze());
    }

    /*C: We can't check for freezing directly at the end of the move because even though OnSTileMoveEndEarly and OnSTileMoveEnd
         are both called after the tile is moved into place, it seems as if the colliders don't update quickly enough.
         (might have something do with the fact that physics calculations happen early on, so maybe it isn't updated until next frame?)
         Either way after copious amounts of testing this combination of OnSTileMoveEndEarly with this tiny delay
         makes refreezing work as intended. 
    */
    IEnumerator WaitThenCheckFreeze() 
    {
        yield return new WaitForSeconds(0.03f);
        if(CheckFreeze())
                Freeze();
    }

    public bool CheckFreeze()
    {
        return (!refreezeOnTop || sTile.y > 1) && (!anchorBroken || refreezeFromBroken) && numLavaSources <= 0;
    }

    public void Break(bool fromLoad = false) {
        if(fromLoad || ((isFrozen || canBreakWhenNotFrozen) && canBreakWithAnchor && !anchorBroken))
        {
            anchorBroken = true;
            numTimesBroken++;
            isFrozen = false;
           // animator.SetBool("Broken", true);
            if(spriteRenderer)
                spriteRenderer.sprite = anchorBrokenSprite;
            onBreak.Invoke();
        }
    }

    public void Melt(bool fromLoad = false)
    {
        if(fromLoad || (isFrozen && numLavaSources > 0)) 
        {
            isFrozen = false;
            if(spriteRenderer)
                spriteRenderer.sprite = meltedSprite;
            onMelt.Invoke();
        }
    }

    public void Freeze(bool fromLoad = false)
    {
        if(fromLoad || !isFrozen)
        {
            isFrozen = true;
            if(spriteRenderer)
                spriteRenderer.sprite = frozenSprite;
            onFreeze.Invoke();
        }
    }

    public void Fix()
    {
        if(anchorBroken) 
        {
            anchorBroken = false;
           // animator.SetBool("Broken", false);
            if(fixBackToFrozen)
                Freeze();
            else if(spriteRenderer)
                spriteRenderer.sprite = meltedSprite;
            onFix.Invoke();
        }
    }

    public void RemoveLava()
    {
        numLavaSources--;
    }

    public void AddLava()
    {
        numLavaSources++;
        Melt();
    }

    public void Save()
    {
        SaveSystem.Current.SetBool(gameObject.name + "Frozen", isFrozen);
        SaveSystem.Current.SetBool(gameObject.name + "Broken", anchorBroken);
    }

    public void Load(SaveProfile profile)
    {
        isFrozen = profile.GetBool(gameObject.name + "Frozen", true);
        anchorBroken = profile.GetBool(gameObject.name + "Broken");
        if(isFrozen)
            Freeze(true);
        else if(anchorBroken)
            Break(true);
        else
            Melt(true);
    }

    public void IsFrozen(Condition c) {
        c.SetSpec(isFrozen);
    }

    public void IsNotFrozen(Condition c) {
        c.SetSpec(!isFrozen);
    }

    public void IsNotFrozenOrBroken(Condition c) {
        c.SetSpec(!isFrozen && !anchorBroken);
    }

    public bool IsNotFrozenOrBroken() {
        return(!isFrozen && !anchorBroken);
    }

    public void HasBeenBrokenMultipleTimes(Condition c){
        c.SetSpec(numTimesBroken > 1);
    }
}
