using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Souvenir;
using UnityEngine;
using static Souvenir.AnswerLayout;

public enum SPointOfOrder
{

    [SouvenirQuestion("What was one of the previously played cards in {0}?", ThreeColumns6Answers, ExampleAnswers = new[] { "A♠", "A♥", "A♣", "A♦", "2♠", "2♥", "2♣", "2♦" })]
    PreviousCards,
}

public partial class SouvenirModule
{
    [SouvenirHandler("PointOfOrderModule", "Point Of Order", typeof(SPointOfOrder), "Hawker")]
    private IEnumerator<SouvenirInstruction> ProcessPointOfOrder(ModuleData module)
    {
        var comp = GetComponent(module, "PointOfOrderModule");
        var pileFld = GetField<IList>(comp, "_pile");
        //flip the cards over
        module.Module.OnPass += () =>
        {
            int num = 5;
            Transform[] displayCardTransforms = Enumerable.Range(1, num).Select(ix => module.Module.transform.Find($"PileCard{ix}")).ToArray();
            GameObject backCardCopy = module.Module.transform.Find("ChoiceCard1/Card/BackFace").gameObject;
            var animationLoop = GetMethod<IEnumerable>(comp, "animationLoop", 4);
            var flipCard = GetMethod<IEnumerator>(comp, "flipCard", 3);
            IEnumerator FlipDisplayCard(int index)
            {
                Transform card = displayCardTransforms[index];
                yield return new WaitForSeconds(.2f * index);

                //play the flipping sound 
                Sounds.PlayForeignClip("PointOfOrder", "cardflip", card.transform);
                Quaternion initialRotation = card.rotation;
                Vector3 initalPosition = card.localPosition + new Vector3(0, .001f, 0);
                Action<float> action = i =>
                {
                    card.rotation = initialRotation * Quaternion.AngleAxis(i, Vector3.up);
                    card.localPosition = initalPosition + new Vector3(0, -i * (i - 180) * .00001f, 0);
                };

                foreach (var _ in animationLoop.Invoke(0, 180, 360, action))
                    yield return _;
            }
            for (int i = 0; i < num; i++)
            {
                if (i < num - 1)
                {
                    //flip the chosen cards over
                    StartCoroutine(flipCard.Invoke(i, true, false));
                }

                //give the display cards a back and flip it over
                Transform back = Instantiate(backCardCopy, displayCardTransforms[i]).transform;
                back.localRotation = Quaternion.Euler(180, 0, 0);
                back.localScale = Vector3.one;
                StartCoroutine(FlipDisplayCard(i));
            }

            return false;
        };
        yield return WaitForSolve;

        var pile = pileFld.Get();
        var allCards = GetStaticField<IList>(pile[0].GetType(), "AllCards", isPublic: true).Get();
        var allAnswers = new string[52];
        var answers = new string[5];
        for (int i = 0; i < answers.Length; i++)
        {
            answers[i] = pile[i].ToString();
        }
        for (int i = 0; i < allAnswers.Length; i++)
        {
            allAnswers[i] = allCards[i].ToString();
        }
        yield return question(SPointOfOrder.PreviousCards).Answers(answers, all: allAnswers);
    }
}
