using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System;
using System.Reflection;
using System.IO;
using UnityEngine;

[Serializable]
public static class EventManager
{
    private static List<SimulationEvent> m_knownEvents = new List<SimulationEvent>();
    private static List<SimulationEvent> m_hiddenEvents = new List<SimulationEvent>();
    private static List<SimulationEvent> m_reservedEvents = new List<SimulationEvent>();
    private static List<Ability> m_abilities = new List<Ability>();

    static EventManager()
    {

        TextAsset tmp = Resources.Load("events") as TextAsset;
        TextReader reader = new StringReader(tmp.text);
        XDocument topLevel = XDocument.Load(reader);
        List<XElement> allEvents = topLevel.Root.Descendants("event").ToList();

        foreach (XElement simEvent in allEvents)
        {
            string qualification = simEvent.Attributes("qualification").Count() == 0 ? "NONE" : simEvent.Attribute("qualification").Value;

            Requirement eventRequirements = new Requirement(Convert.ToBoolean(Int32.Parse(simEvent.Attribute("req_children").Value) & 1),
                                                            Convert.ToBoolean(Int32.Parse(simEvent.Attribute("req_parent").Value) & 1),
                                                            Convert.ToBoolean(Int32.Parse(simEvent.Attribute("req_grandpa").Value) & 1),
                                                            Convert.ToBoolean(Int32.Parse(simEvent.Attribute("req_money").Value) & 1),
                                                            Convert.ToBoolean(Int32.Parse(simEvent.Attribute("req_accept").Value) & 1),
                                                            Convert.ToBoolean(Int32.Parse(simEvent.Attribute("req_children").Value) >> 1 & 1),
                                                            Convert.ToBoolean(Int32.Parse(simEvent.Attribute("req_parent").Value) >> 1 & 1),
                                                            Convert.ToBoolean(Int32.Parse(simEvent.Attribute("req_grandpa").Value) >> 1 & 1),
                                                            Qualification.GetQualificationByString(qualification),
                                                            simEvent.Attributes("age").Count() == 0 ? null : Convert.ToString(simEvent.Attribute("age").Value)
                                                            );

			Debug.Log(simEvent.Attribute("name").Value);
            switch (Int32.Parse(simEvent.Attribute("type").Value))
            {
                case (int)Enums.EventType.HIDDEN:
                    m_hiddenEvents.Add(new SimulationEvent( eventRequirements,
                                                            Double.Parse(simEvent.Attribute("chance").Value),
                                                            simEvent.Attribute("name").Value,
                                                            simEvent.Attribute("description").Value,
                                                            Int32.Parse(simEvent.Attribute("id").Value),
                                                            (int)Enums.EventType.HIDDEN,
                                                            Int32.Parse(simEvent.Attribute("priority").Value),
                                                            simEvent.Attributes("month").Count() == 0 ? "0" : simEvent.Attribute("month").Value
                                                            ));
                    break;
                case (int)Enums.EventType.KNOWN:
                    m_knownEvents.Add(new SimulationEvent(eventRequirements,
                                                            1.0,
                                                            simEvent.Attribute("name").Value,
                                                            simEvent.Attribute("description").Value,
                                                            Int32.Parse(simEvent.Attribute("id").Value),
                                                            (int)Enums.EventType.KNOWN,
                                                            Int32.Parse(simEvent.Attribute("priority").Value),
                                                            simEvent.Attribute("month").Value,
                                                            simEvent.Attributes("day").Count() == 0 ? 0 : Int32.Parse(simEvent.Attribute("day").Value),
                                                            simEvent.Attributes("year").Count() == 0 ? 0 : Int32.Parse(simEvent.Attribute("year").Value)
                                                            ));
                    break;
                case (int)Enums.EventType.RESERVED:
                    m_reservedEvents.Add(new SimulationEvent(eventRequirements,
                                                            Double.Parse(simEvent.Attribute("chance").Value),
                                                            simEvent.Attribute("name").Value,
                                                            simEvent.Attribute("description").Value,
                                                            Int32.Parse(simEvent.Attribute("id").Value),
                                                            (int)Enums.EventType.RESERVED,
                                                            Int32.Parse(simEvent.Attribute("priority").Value),
                                                            simEvent.Attributes("month").Count() == 0 ? "0" : simEvent.Attribute("month").Value,
                                                            simEvent.Attributes("day").Count() == 0 ? 0 : Int32.Parse(simEvent.Attribute("day").Value),
                                                            simEvent.Attributes("year").Count() == 0 ? 0 : Int32.Parse(simEvent.Attribute("year").Value)
                                                            ));
                    break;
                case (int)Enums.EventType.ABILITY:
                    m_abilities.Add(new Ability(simEvent.Attribute("ability_name").Value,
                                                simEvent.Attribute("ability_description").Value,
                                                Int32.Parse(simEvent.Attribute("cooldown").Value),
                                                simEvent.Attribute("picture").Value,
                                                Convert.ToDouble(simEvent.Attribute("insanity_cost").Value),
                                                Int32.Parse(simEvent.Attribute("money_cost").Value),
                                                new SimulationEvent(eventRequirements, 0,
                                                                    simEvent.Attribute("name").Value,
                                                                    simEvent.Attribute("description").Value,
                                                                    Int32.Parse(simEvent.Attribute("id").Value), (int)Enums.EventType.ABILITY, 2)
                                               ));

                    break;
                default:
                    break;
            }
        }
    }

    public static Ability GetAbilityById(int id)
    {
        foreach(Ability ability in m_abilities)
        {
            if (ability.Event.EventId == id)
                return ability;
        }
        return null;
    }

    public static SimulationEvent GetRandomEvent()
    {
        int rand = Constants.RANDOM.Next(m_hiddenEvents.Count);
        return m_hiddenEvents[rand];
    }

    public static List<SimulationEvent> GetAllHiddenEvents()
    {
        return m_hiddenEvents;
    }

    public static List<SimulationEvent> GetEventsByDate(int month, int day, int year)
    {
        List<SimulationEvent> eventsOnDay = new List<SimulationEvent>();
        foreach(SimulationEvent ev in m_knownEvents)
        {
            if (ev.EventMonth == month && ev.EventDay == day)
                if (ev.EventYear != 0 && year == ev.EventYear)
                    eventsOnDay.Add(ev);
                else if (ev.EventYear == 0)
                    eventsOnDay.Add(ev);
        }
        foreach (SimulationEvent ev in m_reservedEvents)
        {
            if (ev.EventMonth == month && ev.EventDay == day)
                if (ev.EventYear != 0 && year == ev.EventYear)
                    eventsOnDay.Add(ev);
                else if (ev.EventYear == 0)
                    eventsOnDay.Add(ev);
        }
        return eventsOnDay;
    }

    public static SimulationEvent GetEventById(int id)
    {
        foreach (SimulationEvent ev in m_hiddenEvents)
            if (ev.EventId == id)
                return ev;
        foreach (SimulationEvent ev in m_knownEvents)
            if (ev.EventId == id)
                return ev;
        foreach (SimulationEvent ev in m_reservedEvents)
            if (ev.EventId == id)
                return ev;
        return null;
    }

    public static SimulationEvent GetSystemEventById(int id)
    {
        foreach (SimulationEvent ev in m_reservedEvents)
            if (ev.EventId == id)
                return ev;
        return null;
    }

    public static List<SimulationEvent> GetEventsByMonth(int month)
    {
        List<SimulationEvent> eventsInMonth = new List<SimulationEvent>();
        foreach(SimulationEvent ev in m_knownEvents)
        {
            if (month >= ev.EventMonth && month <= ev.EventMonthMax && ev.EventDay == 0)
            {
                int rand = Constants.RANDOM.Next(ev.EventMonth, ev.EventMonthMax);          //choose a random number between the months the ev occurs e.g. 2-6
                if (rand == (ev.EventMonth + ev.EventMonthMax) / 2)                         //if rand <= 4, let add it for the current month
                    eventsInMonth.Add(ev);
                else
                    ev.EventMonth++;                                                        //else move its month up by one. this way if EventMonth == EventMonthMax it will always be added
            }
        }
        return eventsInMonth;
    }

    public static Func<DataManager, Requirement, Outcome> GetEventFunctionById(int eventId)
    {
        string eventMethodName = "Event" + eventId.ToString();
        MethodInfo methodInfo = typeof(EventManager).GetMethod(eventMethodName);
        return (Func<DataManager, Requirement, Outcome>)Delegate.CreateDelegate(typeof(Func<DataManager, Requirement, Outcome>), methodInfo);
    }


    /* PLAYER DEFINED EVENTS BEYOND THIS POINT ONLY!!
     * PLEASE ADD APPROPRIATE FUNCTION HEADERS WITH COLLOQUIAL INFO 
     * EVENT FUNCTION BANE /MUST/ BE OF THE FORMAT Event[EVENTID] */

    //NAME: WEEKLY STAT UPGRADE DO NOT CHANGE FROM Event0
    public static Outcome Event0(DataManager manager, Requirement requirements)
    {
        foreach(Family leagueFam in manager.LeagueFamilies)
        {
            leagueFam.ApplyStatUpgrades();
        }
		manager.PlayerFamily.ApplyStatUpgrades();
        Outcome statOutcome = new Outcome();
        statOutcome.Status = (int)Enums.EventOutcome.SUCCESS;
        statOutcome.Mail = new Mail();
        statOutcome.Mail.Date = manager.Calendar.GetCurrentDay();
        statOutcome.Mail.Subject = "Your Social Security Check";
        statOutcome.Mail.Sender = manager.PlayerFamily.Parents[0].Name;
        statOutcome.Mail.Message = string.Format(
			"Hey Dad,\n\n\t" +
			"Here is your social security check for the month plus a little something extra I scraped up for you after taking what we needed to keep the family going. The kids are doing just fine " +
			"and are growing so fast! You'll barely even recognize them soon (between you and me, I'm worred {0} is getting uglier by " +
			"the day). Anyway have a good month!\n\nTotal Income: ${3}\nTotal Family Expenses: ${4}.00\nTotal Amount Applied to Account: ${1}.00\n\nLove,\n{2}",
			manager.PlayerFamily.Children[0].Name, manager.PlayerFamily.Grandpa.MoneyGrowth - manager.PlayerFamily.Upkeep, manager.PlayerFamily.Parents[0].Name, manager.PlayerFamily.Grandpa.MoneyGrowth, manager.PlayerFamily.Upkeep);
        return statOutcome;
    }

    //NAME: TRADE_ACCEPT_REJECT
    public static Outcome Event1(DataManager manager, Requirement requirements)
    {
        Outcome tradeOutcome = requirements.Trade[0].PerformTradeAction(manager);
        requirements.Trade.Remove(requirements.Trade[0]);
        return tradeOutcome;
    }

    //NAME: CPS_MONTHLY_EVENT
    public static Outcome Event2(DataManager manager, Requirement requirements)
    {
        Outcome cpsOutcome = new Outcome();
        string familyStatusString;
        int familyStatus;
        manager.PlayerFamily.CalculateChemistry();
        if (manager.PlayerFamily.Chemistry >= 3)
        {
            familyStatusString = "is doing great and really seems to like being around you,";
            familyStatus = (int)Enums.Status.GOOD;
        }
        else if (manager.PlayerFamily.Chemistry < 3 && manager.PlayerFamily.Chemistry >= 2)
        {
            familyStatusString = "is doing decently well and is within acceptable standards,";
            familyStatus = (int)Enums.Status.OK;
        }
        else if (manager.PlayerFamily.Chemistry < 2 && manager.PlayerFamily.Chemistry >= 1)
        {
            familyStatusString = "is starting to deteriorate (we will be watching you closely),";
            familyStatus = (int)Enums.Status.BAD;
        }
        else
        { 
            familyStatusString = "is in completely deplorable conditions,";
            familyStatus = (int)Enums.Status.HORRIBLE;
        }

        string incomeStatusString;
        int incomeStatus;
        if (manager.PlayerFamily.Grandpa.Money >= 10000)
        {
            incomeStatusString = "to be doing fantastic! I guess the stock market worked out for you after all";
            incomeStatus = (int)Enums.Status.GOOD;
        }
        else if (manager.PlayerFamily.Grandpa.Money < 10000 && manager.PlayerFamily.Grandpa.Money >= 5000)
        {
            incomeStatusString = "stable, saving for retirement really pays off I guess";
            incomeStatus = (int)Enums.Status.OK;
        }
        else if (manager.PlayerFamily.Grandpa.Money < 5000 && manager.PlayerFamily.Grandpa.Money >= 3000)
        {
            incomeStatusString = "is looking a little lackluster. Social security alone isn't enough to get by these days";
            incomeStatus = (int)Enums.Status.BAD;
        }
        else
        { 
            incomeStatusString = "horrible. You're broke";
            incomeStatus = (int)Enums.Status.HORRIBLE;
        }

        if(incomeStatus == (int)Enums.Status.HORRIBLE && familyStatus == (int)Enums.Status.HORRIBLE)
        {
            if (manager.PlayerInfo.FINAL_WARNING)
            {
                manager.Calendar.ScheduleEventInXDays(EventManager.GetEventById(3), 1);
                manager.PlayerInfo.FINAL_WARNING = false;
                cpsOutcome.Status = (int)Enums.EventOutcome.SUCCESS;
            }
            else
            {
                manager.Calendar.ScheduleEventInXDays(EventManager.GetEventById(4), 1);
                cpsOutcome.Status = (int)Enums.EventOutcome.SUCCESS;
            }
        }

        string subjectStatus = "";
        if (incomeStatus + familyStatus < 2)
            subjectStatus = "GREAT";
        else if(incomeStatus + familyStatus < 4)
            subjectStatus = "GOOD";
        else if (incomeStatus + familyStatus < 6)
            subjectStatus = "O.K.";
        else
            subjectStatus = "HORRIBLE";


        cpsOutcome.Mail = new Mail();
        cpsOutcome.Mail.Date = manager.Calendar.GetCurrentDay();
        cpsOutcome.Mail.Subject = string.Format("Quarterly CPS Update: {0}", subjectStatus);
        cpsOutcome.Mail.Sender = "Charlene Dogood";
        cpsOutcome.Mail.Message = string.Format(
            "Hello Mr. {0},\n\n\t" +
            "This is Ms. Dogood from the Child Protection Services, performing your quarterly scheduled check-in as laid out in the terms of your 1993 release from Leagueville County Prison. " +
            "Currently your family {1} and your income seems {2}. We may have to perform additional check-ins during the month if this is not up to our standards. Until next time.\n\nCordially,\nCharlene Dogood",
            manager.PlayerFamily.FamilyName, familyStatusString, incomeStatusString);

        manager.Calendar.ScheduleEventInXDays(EventManager.GetEventById(2), 84);

        return cpsOutcome;
    }

    //NAME: CPS_FINAL_WARNING
    public static Outcome Event3(DataManager manager, Requirement requirements)
    {
        Outcome cpsOutcome = new Outcome();
        cpsOutcome.Status = (int)Enums.EventOutcome.SUCCESS;
        cpsOutcome.Mail = new Mail();
        cpsOutcome.Mail.Date = manager.Calendar.GetCurrentDay();
        cpsOutcome.Mail.Subject = "URGENT: Child Protection Services";
        cpsOutcome.Mail.Sender = "Offices of the Leagueville CPS";
        cpsOutcome.Mail.Message = string.Format(
            "Mr. {0},\n\n\t" +
            "This is your FINAL NOTICE being served directly from the County CPS and Second Court of Appeals. If the conditions in your family do not improve we will be forced to step in and make things improve." +
            "\n\nWarm Regards,\nJudge Charles Stopbad", manager.PlayerFamily.Grandpa.Name);
        return cpsOutcome;
    }

    //NAME: CPS_RECLAIM
    public static Outcome Event4(DataManager manager, Requirement requirements)
    {
        Outcome cpsOutcome = new Outcome();

        if (manager.PlayerFamily.Children.Count <= 1)
        {
            cpsOutcome.Status = (int)Enums.EventOutcome.PASS;
            return cpsOutcome;
        }

        int random = Constants.RANDOM.Next(0, manager.PlayerFamily.Children.Count - 1);
        string childName = manager.PlayerFamily.Children[random].Name;
        Child remChild = manager.PlayerFamily.Children[random];
        manager.Orphanage.Children.Add(remChild);
        manager.PlayerFamily.Children.Remove(remChild);
        remChild = null;

        cpsOutcome.OutcomeDescription = string.Format("You hear a screech, then a crash. Before you realize it, 20 Leagueville SWAT Team members are surrounding your house. Charlene Dogood walks through the front door, " +
                                        "picks up {0} and with quite a bit of effort (and help) drags them into the back of the SWAT van. Then silence.", childName);
        cpsOutcome.Mail = new Mail();
        cpsOutcome.Status = (int)Enums.EventOutcome.SUCCESS;
        cpsOutcome.Mail.Date = manager.Calendar.GetCurrentDay();
        cpsOutcome.Mail.Subject = string.Format("CPS at the {0} residence", manager.PlayerFamily.FamilyName);
        cpsOutcome.Mail.Sender = "Charlene";
        cpsOutcome.Mail.Message = string.Format(
            "Mr. {0},\n\n\t" +
            "It is with the upmost displeasure that I visited your child's deplorable home today. You should be ashamed of the conditions you let your grandchildren live in. " +
            "I am writing this to you as a courtesy to let you know {1} will be with us until further notice. You're lucky I remember that night in Rio in 1997 or else I'd have you and your children locked up." +
            "\n\nGood Day,\nCharlene Dogood", manager.PlayerFamily.Grandpa.Name, childName);
        return cpsOutcome;
    }

    //NAME: TEST EVENT 1
    public static Outcome Event10(DataManager manager, Requirement requirements)
    {
        manager.PlayerFamily.Grandpa.Money += 1000;

        return new Outcome((int)Enums.EventOutcome.SUCCESS, String.Format("grandpa_won_lottery: {0}", manager.PlayerFamily.Grandpa.Money));
    }

    //NAME: TEST EVENT 2
    public static Outcome Event11(DataManager manager, Requirement requirements)
    {
        if (requirements.Accept)
        {
            manager.PlayerFamily.Grandpa.Name = "Leeroy Jenkins";
            return new Outcome((int)Enums.EventOutcome.SUCCESS, String.Format("Grandpa renamed {0}", manager.PlayerFamily.Grandpa.Name));

        }
        return new Outcome((int)Enums.EventOutcome.FAILURE, "BOO!");
    }

    //NAME: TUTORIAL MAIL 1
    public static Outcome Event20(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();
        ret.Status = (int)Enums.EventOutcome.PASS;

        ret.Mail = new Mail();
        ret.Mail.Date = manager.Calendar.GetCurrentDay();
        ret.Mail.Subject = "Day One";
        ret.Mail.Sender = manager.PlayerFamily.Grandpa.Name;
        ret.Mail.Image = "tutorial_1";
        ret.Mail.Message = string.Format(
			"They locked me up in here. They think I'm crazy, but only I know the truth! The truth about everything! They'll see... {0} will see... " +
			"No one can stop me from being the best!!!\nI have the entire year planned out... As much as I hate to say it, my thoughts wander at times... " +
			"I've written down what will happen each day on the green squares on my calendar! I'll need to click and take a closer look as those days approach. The current day is " +
			"the red date... Or was it polka dotted...? Whichever!\nTake care of yourself, me.\n{1}", 
			manager.PlayerFamily.Parents[0].Name, manager.PlayerFamily.Grandpa.Name);

        return ret;
    }

    //NAME: TUTORIAL MAIL 2
    public static Outcome Event21(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();
        ret.Status = (int)Enums.EventOutcome.PASS;

        ret.Mail = new Mail();
        ret.Mail.Date = manager.Calendar.GetCurrentDay();
        ret.Mail.Subject = "Day Eight";
        ret.Mail.Sender = manager.PlayerFamily.Grandpa.Name;
        ret.Mail.Image = "tutorial_2";
        ret.Mail.Message = string.Format(
			"My damn children and grandkids are nothing but disappointments! I've about had it with those whippersnappers. I'll have to provide each with careful " +
			"instruction on how to lead me to victory over these other old devils! But my family's brains are like tiny hamster brains... They can only focus on one " +
			"skill at a time by clicking on them on the Family Panel... Damn millennials! Back in my day we had six year olds studing astrophysics in Latin, all while earning $0.62 a month.\nGod speed.\n{0}", 
			manager.PlayerFamily.Grandpa.Name);

        return ret;
    }

    //NAME: TUTORIAL MAIL 3
    public static Outcome Event22(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();
        ret.Status = (int)Enums.EventOutcome.PASS;

        ret.Mail = new Mail();
        ret.Mail.Date = manager.Calendar.GetCurrentDay();
        ret.Mail.Subject = "Day Fifteen";
        ret.Mail.Sender = manager.PlayerFamily.Grandpa.Name;
        ret.Mail.Image = "tutorial_3";
        ret.Mail.Message = 
			"Those damn commie relics in the community panel already have a leg up on me. They're cheats, I swear it! No way they could be that proud of their snot-nosed brats. I'm gonna take this " +
			"community by storm! \nI swear I'll win I swear I'll win I swear I'll win I swear I'll win I swear I'll win I swear I'll win I swear I'll win.";

        return ret;
    }

    //NAME: TUTORIAL MAIL 4
    public static Outcome Event23(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();
        ret.Status = (int)Enums.EventOutcome.PASS;

        ret.Mail = new Mail();
        ret.Mail.Date = manager.Calendar.GetCurrentDay();
        ret.Mail.Subject = "Day Twenty-Five";
        ret.Mail.Sender = manager.PlayerFamily.Grandpa.Name;
        ret.Mail.Image = "tutorial_4";
        ret.Mail.Message = string.Format(
			"That's it! I am done with {0} and {1} too! {2}'s grandson already has a full ride to Stanford and he's only {3}! Let's see if we can't pawn off these idiots to some " +
			"unsuspecting grandpas using the adoption panel... A little cash to sweeten the pot wouldn't hurt...", 
			manager.PlayerFamily.Children[0].Name, manager.PlayerFamily.Parents[0].Name, manager.LeagueFamilies[0].Grandpa.Name, manager.LeagueFamilies[0].Children[0].Age);

        return ret;
    }

    //NAME: TUTORIAL MAIL 5 (abilities)
    public static Outcome Event24(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();
        ret.Status = (int)Enums.EventOutcome.PASS;

        ret.Mail = new Mail();
        ret.Mail.Date = manager.Calendar.GetCurrentDay();
        ret.Mail.Subject = "Unlock your inner potential";
        ret.Mail.Image = "tutorial_5";
        ret.Mail.Sender = "???";
        ret.Mail.Message = string.Format(
            "Hey buddy, old pal... Good to see you again! Remember the good old days? Me too, me too. Come on down and I'll help you unlock your inner potential and abilities you never even " +
            "knew you had! Whenever you need help, I'm there for you. I always am, aren't I?\n\nLove,\n???");

        return ret;
    }

    //NAME: INSANITY MAIL 1
    public static Outcome Event40(DataManager manager, Requirement requirements)
	{
		Outcome ret = new Outcome ();
		if (manager.PlayerFamily.Grandpa.Insanity > 30) 
		{
			ret.Status = (int)Enums.EventOutcome.PASS_BLACKLIST_FOREVER;

			ret.Mail = new Mail ();
			ret.Mail.Date = manager.Calendar.GetCurrentDay ();
			ret.Mail.Subject = "My Mental State";
			ret.Mail.Sender = manager.PlayerFamily.Grandpa.Name;
			ret.Mail.Message = string.Format (
				"It's unfortunate, but I can feel my mental state deteriorating at an alarmingly high rate. Some might say it's simply old age... But I know it's something " +
				"they put in the water in this damned facility. That or one of my despicable rivals... I'll have to be more careful from now on...");
		}
		else
			ret.Status = (int)Enums.EventOutcome.PASS;
		
		return ret;
	}

	//NAME: INSANITY MAIL 2
	public static Outcome Event41(DataManager manager, Requirement requirements)
	{
		Outcome ret = new Outcome ();
		if (manager.PlayerFamily.Grandpa.Insanity > 50) 
		{
			ret.Status = (int)Enums.EventOutcome.PASS_BLACKLIST_FOREVER;

			ret.Mail = new Mail ();
			ret.Mail.Date = manager.Calendar.GetCurrentDay ();
			ret.Mail.Subject = "My Mental Stagqfn";
			ret.Mail.Sender = manager.PlayerFamily.Grandpa.Name;
			ret.Mail.Message = string.Format (
				"I see thnigs that I shouldnt be seeing. I could pretned that I'm as sharp as I eevr was, but that would be a lie. There's moevmnet on the edge of my vision. " +
				"I can persevrere, though... I must...");
		}
		else
			ret.Status = (int)Enums.EventOutcome.PASS;

		return ret;
	}

	//NAME: INSANITY MAIL 3
	public static Outcome Event42(DataManager manager, Requirement requirements)
	{
		Outcome ret = new Outcome ();
		if (manager.PlayerFamily.Grandpa.Insanity > 70) 
		{
			ret.Status = (int)Enums.EventOutcome.PASS_BLACKLIST_FOREVER;

			ret.Mail = new Mail ();
			ret.Mail.Date = manager.Calendar.GetCurrentDay ();
			ret.Mail.Subject = "Mu Mengyh Stagqfn";
			ret.Mail.Sender = manager.PlayerFamily.Grandpa.Name;
			ret.Mail.Message = string.Format (
				"I feel the insanity taking hold. Its cold grip. I see myself do things I would never do. Horrible things. Why is this happening? I'm afraid. I miss my wife.");
		}
		else
			ret.Status = (int)Enums.EventOutcome.PASS;

		return ret;
	}

	//NAME: INSANITY MAIL 4
	public static Outcome Event43(DataManager manager, Requirement requirements)
	{
		Outcome ret = new Outcome ();
		if (manager.PlayerFamily.Grandpa.Insanity > 90) 
		{
			ret.Status = (int)Enums.EventOutcome.PASS_BLACKLIST_FOREVER;

			ret.Mail = new Mail ();
			ret.Mail.Date = manager.Calendar.GetCurrentDay ();
			ret.Mail.Subject = "The Grandpa League";
			ret.Mail.Sender = manager.PlayerFamily.Grandpa.Name;
			ret.Mail.Message = string.Format (
				"I feel my sanity almost completely gone. Good riddance. Shackled to morality is no way to play this game. I will be at the top of the league. It is inevitable. " +
				"All others will bow before me! Bow before Grandpa {0}!!!",
				manager.PlayerFamily.Grandpa.Name);
		}
		else
			ret.Status = (int)Enums.EventOutcome.PASS;

		return ret;
	}

	//ABILITY: ABILITY STAT DOUBLE START
	public static Outcome Event60(DataManager manager, Requirement requirements)
	{
		Outcome ret = new Outcome ();
		if (requirements.Accept) 
		{
			manager.Calendar.ScheduleEventInXDays(EventManager.GetEventById(61), 28);

			requirements.Child.AddQualification (Qualification.GetQualificationByString ("STAT_GAINS_DOUBLED"));
            requirements.Child.SetDoubleStatMultiplier(true);

			ret.Status = (int)Enums.EventOutcome.SUCCESS;
			ret.OutcomeDescription = String.Format (
				"{0}'s stat gains are doubled for the next month!", 
				requirements.Child.Name);
		}
		else
			ret.Status = (int)Enums.EventOutcome.PASS;

		return ret;
	}

	//ABILITY: ABILITY STAT DOUBLE END
	public static Outcome Event61(DataManager manager, Requirement requirements)
	{
		Outcome ret = new Outcome ();

		requirements.Child.RemoveQualification (Qualification.GetQualificationByString ("STAT_GAINS_DOUBLED"));
		requirements.Child.SetDoubleStatMultiplier(false);

        ret.Status = (int)Enums.EventOutcome.SUCCESS;
        ret.Mail = new Mail();
        ret.Mail.Date = manager.Calendar.GetCurrentDay();
        ret.Mail.Sender = "???";
        ret.Mail.Subject = "Boost running dry...";
        ret.Mail.Message =  String.Format (
			"Hey pal...{0}'s roid's uh, I mean nutrients should be wearing off soon...I forgot to mention but you may want to keep an eye on them for a little while...May get angry...or worse... Come back if you ever need more...", 
			requirements.Child.Name);

		return ret;
	}

	//ABILITY: EVENT REPLAYER
	public static Outcome Event62(DataManager manager, Requirement requirements)
	{
		Outcome ret = new Outcome ();
		if (requirements.Accept) 
		{
            int day = manager.Calendar.Day == 1 ? 28 : manager.Calendar.Day - 1;
            List<SimulationEvent> prevDay = manager.Calendar.GetEventsForDay(day, manager.Calendar.Month);
            List<SimulationEvent> executed = new List<SimulationEvent>();
            foreach(SimulationEvent ev in prevDay)
            {
                if (ev.FinishedExecution && ev.EventId != 1)
                    executed.Add(ev);
            }

            if (executed.Count == 0)
            {
                ret.Status = (int)Enums.EventOutcome.FAILURE;
                ret.OutcomeDescription = String.Format("Hmm nothing of note happened yesterday...");
                return ret;
            }

            if (manager.BlacklistYear.Contains(executed[0].EventId))
                manager.BlacklistYear.Remove(executed[0].EventId);
            if (manager.BlacklistForever.Contains(executed[0].EventId))
                manager.BlacklistForever.Remove(executed[0].EventId);

            ret.Status = (int)Enums.EventOutcome.SUCCESS;
			ret.OutcomeDescription = String.Format (
				"Grandpa whips out his time machine... Let's try that event again...");
            manager.Calendar.ScheduleEventInXDays(executed[0], 1);
        }
		else
			ret.Status = (int)Enums.EventOutcome.PASS;

		return ret;
	}

	//ABILITY: CHILD SACRIFICE
	public static Outcome Event63(DataManager manager, Requirement requirements)
	{
		Outcome ret = new Outcome ();
		if (requirements.Accept) 
		{
            if(manager.PlayerFamily.Children.Count <= 1)
            {
                ret.Status = (int)Enums.EventOutcome.FAILURE;
                ret.OutcomeDescription = String.Format(
                    "This is my last grandchild...that wouldn't help me win...");
                return ret;
            }
			manager.PlayerFamily.Children.Remove (requirements.Child);
            manager.PlayerFamily.Grandpa.Insanity = manager.PlayerFamily.Grandpa.Insanity * 0.20;
            manager.PlayerFamily.Grandpa.Insanity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

            ret.Status = (int)Enums.EventOutcome.SUCCESS;
			ret.OutcomeDescription = String.Format (
				"Grandpa lets his sacrificial knife clatter to the floor. I am sorry, {0}, but it was the only way...\n\n" +
				"{0} removed from the family!\nGrandpa's Insanity way down!\nGrandpa's Wisdom down!",
				requirements.Child.Name);
		}
		else
			ret.Status = (int)Enums.EventOutcome.PASS;

		return ret;
	}

    //Grandkid wanders into traffic
    public static Outcome Event102(DataManager manager, Requirement requirements)
    {
        Outcome returnObj = new Outcome();

        if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Intelligence, (int)Enums.Difficulty.EASY))
        {
            manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
            requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

            returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
            returnObj.OutcomeDescription = String.Format(
                "While {0} was playing playing kickball with the neighborhood kids, the ball accidentaly went rolling into street. " +
                "When a car came roaring down the road, {0} pulled {1} unaware friend out of the way just before he got hit! " +
                "{0} is now a neighborhood hero! \n \n" +
                "{0}'s popularity up! \n" +
                "Grandpa pride up!",
                requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "her" : "his");

        }
        else
        {
            requirements.Child.Athleticism -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
            manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

            returnObj.Status = (int)Enums.EventOutcome.FAILURE;
            returnObj.OutcomeDescription = String.Format(
                "Well, {0} was never known for {1} intelligence. While out playing in the yard one day, " +
				"{2} wandered into traffic to get {1} wayward ball. Unfortunately, the car did not see {1} little body " +
                "in the road in time. It's okay, though. It was just a love tap from the car. However, {1} leg is now screwed up.\n\n" +
                "{0}'s athleticism way down! \n" +
                "Grandpa's pride down!",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "her" : "his", Convert.ToBoolean(requirements.Child.Gender) ? "she" : "he");
        }
        return returnObj;
    }

    // Grandpa buys a car
    public static Outcome Event1001(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (requirements.Accept) 
		{
			if (requirements.Money >= 30000) 
			{
				manager.PlayerFamily.Grandpa.Money -= requirements.Money;

				requirements.Parent.Popularity += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT; 
				manager.PlayerFamily.Grandpa.Pride += Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;

				returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
				returnObj.OutcomeDescription = String.Format (
					"Wow! That car is certainly going to turn heads. Thanks for the contribution, Grandpa!" +
					"{0} sees that you really do care!\n\n" +
					"{0}'s popularity way up!\n" +
					"Grandpa's pride way up!", 
					requirements.Parent.Name);
			} 
			else if (requirements.Money >= 2000) 
			{
				manager.PlayerFamily.Grandpa.Money -= requirements.Money;

				requirements.Parent.Love += Constants.Character.MINOR_STAT_CHANGE_AMOUNT; 
				manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2;

				returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
				returnObj.OutcomeDescription = String.Format (
					"That's not going to buy a nice car, Grandpa. More like a total clunker. Maybe back in your day! I'll take it anyway!\n\n" +
					"{0}'s love up.\n" +
					"Grandpa's pride way up.",
					requirements.Parent.Name);
			}
			else
			{
				manager.PlayerFamily.Grandpa.Insanity += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
				manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;;

				returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
				returnObj.OutcomeDescription = String.Format (
					"That's not nearly enough for a nice car... Are you okay, Grandpa?\n\n" +
					"Grandpa's insanity up.\n" +
					"Grandpa's pride down.");
			}
		} 
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS_BLACKLIST_YEAR;
		
		return returnObj;
	}

	// Grandpa sends family on vacation
	public static Outcome Event1002(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (requirements.Accept) 
		{
			if (requirements.Money >= 2000) 
			{
				foreach (Parent parent in manager.PlayerFamily.Parents) 
				{
					parent.Love += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
					parent.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
				}

				foreach (Child child in manager.PlayerFamily.Children) 
				{
					child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
					child.Athleticism += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
				}

				manager.PlayerFamily.Grandpa.Money -= requirements.Money;
				manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2;

				returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
				returnObj.OutcomeDescription = String.Format (
					"That was the best vacation I've ever been on! We feel much closer as a family and the kids ran around so much " +
					"that they're looking like buff demigods! Thanks, Grandpa!\n\n" +
					"All parents' love up!\n" +
					"All parents' popularity up!\n" +
					"All children's popularity up!\n" +
					"All children's athleticism up slightly.\n" +
					"Grandpa's pride way up.");
			} 
			else
			{
				foreach (Parent parent in manager.PlayerFamily.Parents) 
				{
					parent.Love -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
				}
				manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

				returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
				returnObj.OutcomeDescription = String.Format (
					"That's not nearly enough money for the first class Carribean vacation we had in mind...\n\n" +
					"All parents' love down slightly.\n" +
					"Grandpa's pride down slightly.");
			}
		} 
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS_BLACKLIST_YEAR;
		
		return returnObj;
	}

	// Grandkid tries out for football team
	public static Outcome Event1003(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Athleticism, (int)Enums.Difficulty.VERY_EASY))
		{
			requirements.Child.AddQualification (Qualification.GetQualificationByString ("ON_FOOTBALL_TEAM"));

			requirements.Child.Athleticism += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.AthleticismGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;
			requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.PopularityGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Great ball tossing, {0}! Now everyone will value you more as a person! You'll probably be as buff as Dwayne \"The Rock\" Johnson " +
				"by the end of the season!\n\n" +
				"{0}'s athleticism up.\n" +
				"{0}'s athleticism growth up.\n" + 
				"{0}'s popularity up.\n" +
				"{0}'s popularity growth up.\n" + 
				"Grandpa's pride up.",
				requirements.Child.Name);
		}
		else
		{
			requirements.Child.Popularity -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
			requirements.Child.PopularityGrowth -= Constants.Character.MINOR_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Well, you didn't get on the team. That's disappointing, {0}. Not to mention you looked really dumb out there. Some of the kids are calling you " +
				"Not-So-Sticky-Fingers behind your back!\n\n" +
				"{0}'s popularity down slightly.\n" +
				"{0}'s popularity growth down slightly.\n" + 
				"Grandpa's pride down slightly.",
				requirements.Child.Name);
		}
		return returnObj;
	}

	// Grandkid is dating head cheerleader
	public static Outcome Event1008(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.HARD) && 
			Constants.Roll(requirements.Child.Cuteness, requirements.Child.Athleticism, (int)Enums.Difficulty.STANDARD)) 
		{
			requirements.Child.Popularity += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			requirements.Child.PopularityGrowth += Constants.Character.MAJOR_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Wow, {0} is so popular and talented at indoor football that they are dating the head cheerleader! That's a couple that will be sure to turn " +
				"some heads!\n\n" +
				"{0}'s popularity way up!\n" + 
				"{0}'s popularity growth way up!\n" + 
				"Grandpa's pride way up.",
				requirements.Child.Name);
		}
		else  
			returnObj.Status = (int)Enums.EventOutcome.PASS;
		
		return returnObj;
	}

	// Grandkid critically injured
	public static Outcome Event1009(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (!Constants.Roll(requirements.Child.Cuteness, requirements.Child.Athleticism, (int)Enums.Difficulty.STANDARD))
		{
			requirements.Child.RemoveQualification (Qualification.GetQualificationByString ("ON_FOOTBALL_TEAM"));

			requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.Athleticism -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT * 2;
			requirements.Child.AthleticismGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Oh God, seeing your leg fly right out of the socket during that indoor football game was brutal! I guess you're off the indoor football team for the year.\n\n" +
				"{0}'s popularity up.\n" +
				"{0}'s athleticism way way down!\n" +
				"{0}'s athleticism growth down.\n" +
				"Grandpa's pride down slightly.",
				requirements.Child.Name);
		}
		else  
			returnObj.Status = (int)Enums.EventOutcome.PASS;
		
		return returnObj;
	}

	// Grandkid great at practice
	public static Outcome Event1010(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Athleticism, (int)Enums.Difficulty.HARD))
		{
			requirements.Child.Athleticism += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.AthleticismGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"Holy cow, {0} is just kicking ass at indoor football practice. It's clear to everyone that they're a very skilled player!\n\n" +
				"{0}'s athleticism up.\n" +
				"{0}'s athleticism growth up.\n" +
				"Grandpa's pride up.",
				requirements.Child.Name);
		}
		else  
			returnObj.Status = (int)Enums.EventOutcome.PASS;
		
		return returnObj;
	}

	// Grandpa buys experimental football gear
	public static Outcome Event1011(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (requirements.Accept && manager.PlayerFamily.Grandpa.Money >= 500) 
		{
			requirements.Child.Athleticism += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			requirements.Child.AthleticismGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			requirements.Child.AddQualification (Qualification.GetQualificationByString ("ILLEGAL_GEAR"));

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Money -= 500;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Those Cambodians never disappoint! {0} is untouchable at practice now! {1} can slip through even the tightest of defenses. " +
				"I just hope no one finds out our dirty little secret...\n\n" +
				"{0}'s athleticism up.\n" +
				"{0}'s athleticism growth up.\n" +
				"Grandpa's pride up.",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "She" : "He");
		}
		else if (requirements.Accept && manager.PlayerFamily.Grandpa.Money < 500) 
		{
			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"\"Hey, what are you trying to pull, gramps? You don't have the cash...\"\n\n");
		}
		else
			returnObj.Status = (int)Enums.EventOutcome.PASS_BLACKLIST_YEAR;
		
		return returnObj;
	}

	// Grandkid's illegal gear discovered
	public static Outcome Event1012(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (requirements.Accept && manager.PlayerFamily.Grandpa.Money >= 1000) 
		{
			manager.PlayerFamily.Grandpa.Money -= 1000;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"\"Nice doing business with you, gramps!\"\nDamn those Cambodians... \nDamn them straight to hell...");
		}
		else  
		{
			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;

			requirements.Child.RemoveQualification (Qualification.GetQualificationByString ("ILLEGAL_GEAR"));
			requirements.Child.RemoveQualification (Qualification.GetQualificationByString ("ON_FOOTBALL_TEAM"));

			requirements.Child.AthleticismGrowth -= Constants.Character.MAJOR_STAT_GROWTH_AMOUNT;
			requirements.Child.Athleticism -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.Popularity -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2;

			returnObj.OutcomeDescription = String.Format (
				"You're gonna regret that pal! \n\"{0}, you're off the team!\"\nOh! My pride! It physically hurts!\n\n" +
				"{0}'s athleticism down.\n" +
				"{0}'s athleticism growth down.\n" +
				"{0}'s popularity way down!\n" +
				"Grandpa's pride way down!",
				requirements.Child.Name);
		}
		return returnObj;
	}

	// Grandkid's football championship
	public static Outcome Event1013(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Athleticism, (int)Enums.Difficulty.VERY_HARD)) 
		{
			requirements.Child.Athleticism += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			requirements.Child.AthleticismGrowth += Constants.Character.MAJOR_STAT_GROWTH_AMOUNT;

			requirements.Child.Popularity += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			requirements.Child.PopularityGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;

			requirements.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2;
			requirements.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"It's the most intense game of indoor football ever displayed! {0} scores every goal personally. " +
				"The entire indoor stadium are on their feet except {1}. During the last goal, {1}'s son is taken off the field on a stretcher! " +
				"But {0}'s team wins in the end. Of course.\n\n" +
				"{0}'s athleticism way up!\n" +
				"{0}'s athleticism growth way up!\n" +
				"{0}'s popularity way up!\n" +
				"{0}'s popularity growth up.\n" +
				"{1}'s pride way down!\n" +
				"Grandpa's pride way way up!!\n",
				requirements.Child.Name, requirements.Grandpa.Name);
		} 
		else if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Athleticism, (int)Enums.Difficulty.STANDARD)) 
		{
			requirements.Child.Athleticism += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.AthleticismGrowth += Constants.Character.MINOR_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"{1}'s son's team takes an early lead. {0} puts on a respectable display of resistance. {2} even puts {3} elbow through " +
				"some poor kid's face! That's one for the highlight reel! Ultimately, {0} loses... But Grandpa is still proud! Maybe next year.\n\n" +
				"{0}'s athleticism up.\n" +
				"{0}'s athleticism growth up slightly.\n" +
				"Grandpa's pride way up!\n",
				requirements.Child.Name, requirements.Grandpa.Name, Convert.ToBoolean(requirements.Child.Gender) ? "She" : "He", Convert.ToBoolean(requirements.Child.Gender) ? "her" : "his");
		} 
		else 
		{
			requirements.Child.Athleticism -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.AthleticismGrowth -= Constants.Character.MINOR_STAT_GROWTH_AMOUNT;

			requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Uhh, are you sure {0} was practicing all those times they said they were heading to practice? You really couldn't tell by their display " +
				"on the field. Utterly embarassing. Grandpa had to slink out the back at the end of the first quarter. {1} will never let him forget it.\n\n" +
				"{0}'s athleticism down.\n" +
				"{0}'s athleticism growth down.\n" +
				"{0}'s popularity down.\n" +
				"Grandpa's pride down.",
				requirements.Child.Name, requirements.Grandpa.Name);
		}
		requirements.Child.RemoveQualification (Qualification.GetQualificationByString ("ILLEGAL_GEAR"));
		requirements.Child.RemoveQualification (Qualification.GetQualificationByString ("ON_FOOTBALL_TEAM"));
		return returnObj;
	}

	// Grandkid sucks at football
	public static Outcome Event1014(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (!Constants.Roll(requirements.Child.Cuteness, requirements.Child.Athleticism, (int)Enums.Difficulty.EASY))
		{
			requirements.Child.AthleticismGrowth -= Constants.Character.MINOR_STAT_GROWTH_AMOUNT;
			requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"Wow, there's no way to put this lightly so I'm just going to come out and say it. {0} is just completely awful at football. " +
				"I'm not sure what happened. They got on the team okay. Now all the other kids laugh and throw balls at {1}. We can't kick {1} so late into the season, though...\n\n" +
				"{0}'s athleticism growth down.\n" +
				"{0}'s popularity down.\n" +
				"Grandpa's pride down.",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "her" : "him");
		}
		else  
			returnObj.Status = (int)Enums.EventOutcome.PASS;
		
		return returnObj;
	}

	// Grandpa inspects his social security benefits
	public static Outcome Event1004(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Wisdom, (int)Enums.Difficulty.STANDARD))
		{
			manager.PlayerFamily.Grandpa.Wisdom += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Insanity -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			int income_gain = Constants.RANDOM.Next (159, 304);
			manager.PlayerFamily.Grandpa.MoneyGrowth += income_gain;
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"Going through your social security payments, you find a nice loophole. That'll increase your monthly income! Thanks Obama!\n\n" +
				"Grandpa's wisdom up slightly.\n" + 
				"Grandpa's insanity down slightly.\n" + 
				"Grandpa's income up by {0} per month!\n" +
				"Grandpa's pride up.",
				income_gain);
		}
		else  
			returnObj.Status = (int)Enums.EventOutcome.PASS;
		
		return returnObj;
	}

	// Grandkid makes honor roll
	public static Outcome Event1005(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Intelligence, (int)Enums.Difficulty.STANDARD)) 
		{
			requirements.Child.Intelligence += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			foreach (Parent parent in manager.PlayerFamily.Parents) 
			{
				parent.Love += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			}

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"{0} has made the honor roll! Wow, you're like a freakin' genius {0}!\n\n" +
				"{0}'s intelligence up.\n" + 
				"All parents' love up.\n" + 
				"Grandpa's pride up.",
				requirements.Child.Name);
		}
		else  
		{
			requirements.Child.Intelligence -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"{0} missed the honor roll... What a dummy...\n\n" +
				"{0}'s intelligence down slightly.\n" + 
				"Grandpa's pride down.",
				requirements.Child.Name);
		}
		return returnObj;
	}

	// Grandkid auditions for play
	public static Outcome Event1006(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.HARD) && 
			Constants.Roll(requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.EASY)) 
		{
			requirements.Child.AddQualification (Qualification.GetQualificationByString ("IN_PLAY"));

			requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.ArtistryGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.PopularityGrowth += Constants.Character.MINOR_STAT_GROWTH_AMOUNT;

			foreach (Parent parent in manager.PlayerFamily.Parents) 
			{
				parent.Love += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			}

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 3;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"{0} got the lead role in the play! As well as several other roles! {1} audition left everyone " +
				"in tears and it instantly began raining outside. They say drama will never be the same.\n\n" +
				"{0}'s artistry up.\n" + 
				"{0}'s artistry growth up.\n" + 
				"{0}'s popularity up.\n" + 
				"{0}'s popularity growth up slightly.\n" + 
				"All parents' love up.\n" + 
				"Grandpa's pride way up!",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "Her" : "His");
		}
		else if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.EASY) || 
			Constants.Roll(requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.HARD)) 
		{
			requirements.Child.AddQualification (Qualification.GetQualificationByString ("IN_PLAY"));

			requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.ArtistryGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"{0} only messed up a handful of lines and fell off the stage only once or twice. Not too shabby of an audition! " +
				"{0} got a minor role in the play!\n\n" +
				"{0}'s artistry up.\n" + 
				"{0}'s artistry growth up.\n" + 
				"Grandpa's pride up.",
				requirements.Child.Name);
		}
		else 
		{
			requirements.Child.Artistry += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
			requirements.Child.Popularity -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Oof, wow, that was really hard to watch. Running off the audition stage in tears will probably hurt your reputation " +
				"a bit.\n\n" +
				"{0}'s artistry slightly up.\n" +
				"{0}'s popularity slightly down.\n" + 
				"Grandpa's pride slightly down.",
				requirements.Child.Name);
		}
		return returnObj;
	}

	// Grandkid forgets lines at practice
	public static Outcome Event1015(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (!Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.STANDARD)) 
		{
			requirements.Child.Artistry -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
			requirements.Child.ArtistryGrowth -= Constants.Character.MINOR_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"{0} shows up to play practice late practically every time and never seems to know {1} lines. " +
				"People are starting to talk...\n\n" +
				"{0}'s artistry slightly down.\n" + 
				"{0}'s artistry growth slightly down.\n" + 
				"Grandpa's pride slightly down.",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "her" : "his");
		}
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS;
		
		return returnObj;
	}

	// Grandkid's opening night
	public static Outcome Event1016(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.HARD)) 
		{
			requirements.Child.RemoveQualification (Qualification.GetQualificationByString ("IN_PLAY"));

			requirements.Child.Artistry += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			requirements.Child.ArtistryGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			requirements.Child.Popularity += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"The play is a smash hit! The standing ovation has been going on for over 20 minutes already. Remarkable! People are shouting {0}'s " +
				"name at the top of their lungs. The lemur foundation can't count all the money they're making. The dream of having lemurs be as common " +
				"as rats is on the way to reality.\n\n" +
				"{0}'s artistry way up!\n" + 
				"{0}'s artistry growth up.\n" +
				"{0}'s popularity way up!\n" + 
				"Grandpa's pride way way up!!",
				requirements.Child.Name);
		}
		else 
		{
			requirements.Child.RemoveQualification (Qualification.GetQualificationByString ("IN_PLAY"));

			requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.PopularityGrowth -= Constants.Character.MINOR_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Well, that could have gone better. People are killing lemurs in the streets. They are calling this the worst play of all time. {0} will never " +
				"live this down at school for being a part of it.\n\n" +
				"{0}'s popularity down!\n" + 
				"{0}'s popularity growth down slightly.\n" + 
				"Grandpa's pride down.",
				requirements.Child.Name);
		}
		return returnObj;
	}

	// Grandpa finds buried gold
	public static Outcome Event1007(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Wisdom, (int)Enums.Difficulty.EASY) && 
			!Constants.Roll(0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.HARD))
		{
			manager.PlayerFamily.Grandpa.Wisdom += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Insanity -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			int found_amount = 1000 + Constants.RANDOM.Next (1000, 2000);
			manager.PlayerFamily.Grandpa.Money += found_amount;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa was looking through his old treasure maps and remembered ${0} worth of confederate gold he buried many years ago! How lucky!\n\n" +
				"Grandpa's wisdom up slightly.\n" + 
				"Grandpa's insanity down slightly.\n" + 
				"Grandpa gained {0} dollars!",
				found_amount);
		}
		else  
			returnObj.Status = (int)Enums.EventOutcome.PASS;
		
		return returnObj;
	}

	// Parent gets a divorce 
	public static Outcome Event1017(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (!Constants.Roll(0, requirements.Parent.Love, (int)Enums.Difficulty.VERY_EASY) && 
			manager.PlayerFamily.Parents.Count > 1) 
		{
			manager.PlayerFamily.Parents.Remove (requirements.Parent);

			string other_parent = "";
			foreach (Parent parent in manager.PlayerFamily.Parents) 
			{
				Debug.Log ("Remaining parent: " + parent.Name);
				other_parent = parent.Name;
				parent.Love -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT * 2;
				parent.AddQualification (Qualification.GetQualificationByString ("IS_SINGLE"));
			}

			manager.PlayerFamily.Grandpa.Pride -=  Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"{0} has had it! What has this family done for {1} anyway?! {0} could have been an astronaut " +
				"or a plumber or a runway model but no... {3} gave it all up... For what?! {0} wants a divorce!! " +
				"{2} and the kids are inconsolable.\n\n" +
				"{0} has left the family!\n" + 
				"{2}'s love way way down!!\n" + 
				"Grandpa's pride way down!",
				requirements.Parent.Name, Convert.ToBoolean(requirements.Parent.Gender) ? "her" : "him", other_parent, 
				Convert.ToBoolean(requirements.Parent.Gender) ? "She" : "He");
		}
		else if (manager.PlayerFamily.Parents.Count > 1)
		{
			foreach (Parent parent in manager.PlayerFamily.Parents) 
			{
				parent.Love += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			}

			manager.PlayerFamily.Grandpa.Wisdom += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"It's obvious to everyone around them how in love {0} and {1} are! Grandpa feels proud for setting them " +
				"up all those years ago despite their protestations!\n\n" +
				"{0}'s love up.\n" + 
				"{1}'s love up.\n" + 
				"Grandpa's wisdom up.\n" + 
				"Grandpa's pride up.",
				manager.PlayerFamily.Parents[0].Name, manager.PlayerFamily.Parents[1].Name);
		}
		else
			returnObj.Status = (int)Enums.EventOutcome.PASS;

        requirements.Parent = null;
		return returnObj;
	}

	// Parent gets in car accident 
	public static Outcome Event1018(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (manager.PlayerFamily.Parents.Count > 1) 
		{
			manager.PlayerFamily.Parents.Remove (requirements.Parent);

			string other_parent = "";
			foreach (Parent parent in manager.PlayerFamily.Parents) {
				Debug.Log ("Remaining parent: " + parent.Name);
				other_parent = parent.Name;
				parent.Love += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
				parent.LoveGrowth -= Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;
				parent.Popularity += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
				parent.AddQualification (Qualification.GetQualificationByString ("IS_SINGLE"));
			}

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"{0} was driving along the precarious cliffs on the edge of town to get the family a pizza and a really " +
				"unfortunately timed gust of wind threw {1} car straight into a bottomless pit. Yo, that, like, totally sucks. " +
				"Sorry.\n\n" +
				"{0} has left the family!\n" +
				"{2}'s love up.\n" +
				"{2}'s love growth down.\n" +
				"{2}'s popularity way up!\n" +
				"Grandpa's pride down!",
				requirements.Parent.Name, Convert.ToBoolean (requirements.Parent.Gender) ? "her" : "his", other_parent);
		}
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS;
		
		return returnObj;
	}

	// Grandkid throws party
	public static Outcome Event1019(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if ((Constants.Roll(requirements.Child.Cuteness, requirements.Parent.Love, (int)Enums.Difficulty.EASY) && 
			(Constants.Roll(requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.HARD) && 
				requirements.Accept)) 
			||
			(Constants.Roll(requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.EASY) && 
				requirements.Money >= 200 && requirements.Accept) 
			||
			(requirements.Money >= 500 && requirements.Accept)) 
		{
			requirements.Child.Popularity += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			requirements.Child.PopularityGrowth += Constants.Character.MAJOR_STAT_GROWTH_AMOUNT;

			requirements.Parent.Popularity += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			requirements.Parent.Love += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Parent.PopularityGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Money -= requirements.Money;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"Holy shit. It's all kind of a blur, but that party was off the hook! Where did that elephant come from? I've never seen Grandpa dance like that. " +
				"It's so cool that your {1} let us throw that! Can't believe Barack Obama showed up. I want to remember last night for the rest of my life. " +
				"You're sooooo cool, {2}!\n\n" +
				"{2}'s popularity way up!\n" +
				"{2}'s popularity growth way up!\n" +
				"{3}'s popularity way up!\n" +
				"{3}'s popularity growth up!\n" +
				"Grandpa's pride way way up!!\n" +
				"Grandpa's insanity up.",
				manager.PlayerFamily.Grandpa.Name, Convert.ToBoolean (requirements.Parent.Gender) ? "mom" : "dad", requirements.Child.Name, requirements.Parent.Name);
		} 
		else if (!requirements.Accept) 
		{
			requirements.Child.Popularity -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"Lame! Don't you ever have any fun? You're wrong, it would have been a totally rad party... \nSniff... \n\n" +
				"{0}'s popularity slightly down.\n",
				requirements.Child.Name);
		} 
		else 
		{
			requirements.Child.Popularity -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			requirements.Child.PopularityGrowth -= Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Wisdom -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.InsanityGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;
			manager.PlayerFamily.Grandpa.Money -= requirements.Money;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"Well, that was the might awkward night of {0}'s life. No amount of money would have made that fun. Who let that kid throw a party? {1}'s totally a loser... And who brought " +
				"that live tiger? Was that you, Grandpa?\n\n" +
				"{0}'s popularity way down!\n" +
				"{0}'s popularity growth down.\n" +
				"Grandpa's wisdom down.\n" +
				"Grandpa's insanity up.\n" +
				"Grandpa's insanity growth up.\n" +
				"Grandpa's pride down.",
				requirements.Child.Name, Convert.ToBoolean (requirements.Child.Gender) ? "She" : "He");
		}

		manager.Calendar.ScheduleEventInXDays(EventManager.GetEventById(1019), 120);
		return returnObj;
	}

	// Parent injured at construction site
	public static Outcome Event1020(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (!Constants.Roll(0, requirements.Parent.Intelligence, (int)Enums.Difficulty.EASY))
		{
			requirements.Parent.Intelligence -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Parent.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"{0} was walking through a construction site the other day and got hit by a girder! Why was {1} even walking through there? " +
				"What a dummy...\n\n" +
				"{0}'s intelligence down.\n" +
				"{0}'s popularity down!\n" +
				"Grandpa's pride down slightly.",
				requirements.Parent.Name, Convert.ToBoolean (requirements.Parent.Gender) ? "she" : "he");
		}
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Grandkid gets into Harvard
	public static Outcome Event1021(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Intelligence, (int)Enums.Difficulty.HARD) && 
			Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.EASY) && 
			Constants.Roll(requirements.Child.Cuteness, requirements.Child.Athleticism, (int)Enums.Difficulty.EASY))
		{
			foreach (Child child in manager.PlayerFamily.Children) 
			{
				child.Intelligence += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
				child.IntelligenceGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;
			}

			foreach (Parent parent in manager.PlayerFamily.Parents) 
			{
				parent.Intelligence += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
				parent.IntelligenceGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;
			}

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_FOREVER;
			returnObj.OutcomeDescription = String.Format (
				"Awesome! {0} just got into Harvard! {1} is going for Nautical Archaeology. It's been {2} passion for as long as you can remember. " +
				"You feel your entire family get smarter just by being in contact with {2}!\n\n" +
				"Entire family's intelligence up!!\n" +
				"Entire family's intelligence growth up!!\n" +
				"Grandpa's pride way up!!",
				requirements.Child.Name, Convert.ToBoolean (requirements.Child.Gender) ? "She" : "He", Convert.ToBoolean (requirements.Child.Gender) ? "her" : "his");
		}
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Grandpa buys family dog
	public static Outcome Event1022(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.HARD))
		{
			requirements.Child.Intelligence -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Wisdom -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 

			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.InsanityGrowth += Constants.Character.MINOR_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa brought home a doggy! We were all so happy until it started foaming at the mouth and bit {1} right in the face! " +
				"Turns out Grandpa found it by the side of the road... Is Grandpa going to be okay, {2}?\n\n" +
				"Grandpa's wisdom down.\n" +
				"Grandpa's insanity up.\n" +
				"Grandpa's insanity growth up slightly.\n" +
				"Grandpa's pride way down!\n" +
				"{1}'s intelligence down.",
				manager.PlayerFamily.Grandpa.Name, requirements.Child.Name, requirements.Parent.Name);
		}
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Grandpa lost at sea
	public static Outcome Event1023(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.HARD))
		{
			manager.PlayerFamily.Grandpa.Wisdom -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 

			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 
			manager.PlayerFamily.Grandpa.InsanityGrowth += Constants.Character.MINOR_STAT_GROWTH_AMOUNT; 

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT; 

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"{1} just got off the phone with the coast guard. They finally found Grandpa. Turns out he wandered into a shipping container " +
				"and got halfway around the world before someone noticed! I'm getting worried about him...\n\n" +
				"Grandpa's wisdom down.\n" +
				"Grandpa's insanity up.\n" +
				"Grandpa's insanity growth up slightly.\n" +
				"Grandpa's pride down.",
				manager.PlayerFamily.Grandpa.Name, requirements.Parent.Name);
		}
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Grandpa gives life advice
	public static Outcome Event1024(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		int roll = Constants.RANDOM.Next (0, 1);
		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Wisdom, (int)Enums.Difficulty.STANDARD) && roll == 0) 
		{
			// Adult
			requirements.Parent.Intelligence += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 
			requirements.Parent.Love += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 
			requirements.Parent.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 

			manager.PlayerFamily.Grandpa.Wisdom += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 

			manager.PlayerFamily.Grandpa.Insanity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT; 

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"{1} was having a really rough day so Grandpa sat {2} down and gave {2} a life advice talk. {1} was completely reinvigorated and has a " +
				"new lease on life! That's the power of Grandpa, bitch!\n\n" +
				"{1} ALL STATS UP!!\n" +
				"Grandpa's wisdom up.\n" +
				"Grandpa's insanity down.\n" +
				"Grandpa's pride up.",
				manager.PlayerFamily.Grandpa.Name, requirements.Parent.Name, Convert.ToBoolean (requirements.Parent.Gender) ? "her" : "him");
		} 
		else if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Wisdom, (int)Enums.Difficulty.STANDARD) && roll == 1) 
		{
			// Child
			requirements.Child.Intelligence += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 
			requirements.Child.Athleticism += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 
			requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 
			requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 
			requirements.Child.Cuteness += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 

			manager.PlayerFamily.Grandpa.Wisdom += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 

			manager.PlayerFamily.Grandpa.Insanity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT; 

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"{1} was having a really rough day so Grandpa sat {2} down and gave {2} a life advice talk. {1} was completely reinvigorated and has a " +
				"new lease on life! That's the power of Grandpa, bitch!\n\n" +
				"{1} ALL STATS UP!!\n" +
				"Grandpa's wisdom up.\n" +
				"Grandpa's insanity down.\n" +
				"Grandpa's pride up.",
				manager.PlayerFamily.Grandpa.Name, requirements.Child.Name, Convert.ToBoolean (requirements.Child.Gender) ? "her" : "him");
		}
		else
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Grandpa loses money
	public static Outcome Event1025(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.HARD))
		{
			double money_lost = manager.PlayerFamily.Grandpa.Money / 2;
			manager.PlayerFamily.Grandpa.Money -= money_lost;

			manager.PlayerFamily.Grandpa.Wisdom -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 
			manager.PlayerFamily.Grandpa.WisdomGrowth -= Constants.Character.MINOR_STAT_GROWTH_AMOUNT; 

			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT; 
			manager.PlayerFamily.Grandpa.InsanityGrowth += Constants.Character.MINOR_STAT_GROWTH_AMOUNT;  

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT; 

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa just lost ${1} on a ponzi scheme! He actually thought he could get rich quick selling plungers door to door. " +
				"Grandpa seems like he's losing more of his independance every day...\n\n" +
				"Grandpa's wisdom down.\n" +
				"Grandpa's wisdom growth down slightly.\n" +
				"Grandpa's insanity up.\n" +
				"Grandpa's insanity growth up slightly.\n" +
				"Grandpa's pride down slightly.\n" +
				"Grandpa lost {1} dollars.",
				manager.PlayerFamily.Grandpa.Name, money_lost.ToString());
		}
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// CHECK: Grandpa commits arson
	public static Outcome Event1030(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.STANDARD))
		{
			manager.Calendar.ScheduleEventInXDays(EventManager.GetEventById(1026), 1);
			returnObj.Status = (int)Enums.EventOutcome.PASS_BLACKLIST_YEAR;
		}
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Grandpa commits arson
	public static Outcome Event1026(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		manager.PlayerFamily.Grandpa.InsanityGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT; 

		manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT; 

		requirements.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2; 

		returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
		returnObj.OutcomeDescription = String.Format (
			"Grandpa just burned down {1}'s house! He was mumbling something about a league when I found him! I don't " +
			"think the police know.\n\n" +
			"Grandpa's insanity growth up.\n" +
			"Grandpa's pride up.\n" +
			"{1} pride way down!",
			manager.PlayerFamily.Grandpa.Name, requirements.Grandpa.Name);

		return returnObj;
	}

	// Grandpa kills a cat
	public static Outcome Event1027(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.STANDARD))
		{
			manager.PlayerFamily.Grandpa.InsanityGrowth += Constants.Character.MINOR_STAT_GROWTH_AMOUNT; 

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT; 

			requirements.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2; 

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Looks like Grandpa strangled {1}'s cat. I found them fighting in our yard this morning. It was not a pretty " +
				"sight, let me tell you. \n\n" +
				"Grandpa's insanity growth up slightly.\n" +
				"Grandpa's pride up slightly.\n" +
				"{1} pride way down!",
				manager.PlayerFamily.Grandpa.Name, requirements.Grandpa.Name);
		}
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Grandpa is naked
	public static Outcome Event1028(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.HARD))
		{
			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Wisdom -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"{0} found Grandpa wandering main street at the crack of dawn, naked as the day he was born. He was staring into " +
				"windows looking for what he called \"Metal Gear\". I have no idea what that means.\n\n" +
				"Grandpa's insanity up.\n" +
				"Grandpa's wisdom down.\n" +
				"Grandpa's pride down slightly.",
				requirements.Parent.Name);
		}
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Grandpa gets a doctor
	public static Outcome Event1029(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (requirements.Accept)
		{
			manager.PlayerFamily.Grandpa.Insanity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Wisdom += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.WisdomGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.MoneyGrowth -= 50;

			returnObj.Mail = new Mail ();
			returnObj.Mail.Date = manager.Calendar.GetCurrentDay ();
			returnObj.Mail.Subject = "RE: Your treatment";
			returnObj.Mail.Sender = "Doctor Fluffernutter";
			returnObj.Mail.Message = string.Format (
				"Grandpa {0},\n\n\t" +
				"Do not be concerned. You are on the road to recovery, my friend. Though I must say, your delusions about a sinister cabal of " +
				"grandpas are most interesting to me. I was hoping I could publish them in a paper! I'll ask you to sign a form next time you're in. " +
				"Anyway, you're going to be fine!\n\nDon't worry,\nDoctor Fluffernutter", 
				manager.PlayerFamily.Grandpa.Name);

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_FOREVER;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa feels much better already! Those pesky voices are just fading into the background! There is no League. " +
				"There is no League. There is no League.\n\n" +
				"Grandpa's insanity down.\n" +
				"Grandpa's wisdom up.\n" +
				"Grandpa's wisdom growth up.\n" +
				"Grandpa's monthly income down $50.");
		}
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS_BLACKLIST_YEAR;

		return returnObj;
	}

	// Grandpa joins a cult
	public static Outcome Event1031(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.HARD))
		{
			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Wisdom -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.MoneyGrowth -= 10;

			returnObj.Mail = new Mail ();
			returnObj.Mail.Date = manager.Calendar.GetCurrentDay ();
			returnObj.Mail.Subject = "RE: The Church of the Tin Can";
			returnObj.Mail.Sender = "Father Cannius";
			returnObj.Mail.Message = string.Format (
				"Grandpa {0},\n\n\t" +
				"I just wanted to send you a quick note to tell you that it's been a pleasure having you in our congregation these last few weeks. " +
				"We just love your old war stories and stories about how cans were invented. Who knew! Just make sure to keep up your monthly payments! " +
				"We are on track to build our new church entirely out of cans!\n\nCan bless,\nFather Cannius", 
				manager.PlayerFamily.Grandpa.Name);

			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_FOREVER;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa just brought over a bunch of pamphlets for The Church of the Tin Can. He's trying to get {0} to wear this weird " +
				"hat. Seems like he's joined a cult. " +
				"He's paying them 10 dollars a month into their Collection Can.\n\n" +
				"Grandpa's insanity up.\n" +
				"Grandpa's wisdom down.\n" +
				"Grandpa's monthly income down $10.",
				requirements.Child.Name);
		}
		else 
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Valentine's Day
	public static Outcome Event1032(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Wisdom, (int)Enums.Difficulty.STANDARD) || 
			Constants.Roll(requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.STANDARD))
		{
			requirements.Child.Popularity += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Wisdom += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa is one smooth operator. He whispers into {0}'s ear the secret word that only grandpas know which will make any {1}'s " +
				"knees weak. It's a critical hit! {0} has a date to the Valentine's Day dance!\n\n" +
				"{0}'s popularity way up!\n" +
				"Grandpa's wisdom up.\n" +
				"Grandpa's pride up.",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "fella" : "lady");
		}
		else if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.HARD))
		{
			requirements.Child.Popularity -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Wisdom -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa, you're nuts!! I never should have listened to you! I knew I shouldn't have put a hornet's nest in {1} locker! " +
				"Hornet's don't even make honey! How was that an acceptable sentiment?! Can you stop drooling and listen to me?\n\n" +
				"{0}'s popularity way down!\n" +
				"Grandpa's insanity up.\n" +
				"Grandpa's wisdom down.\n" +
				"Grandpa's pride down.",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "his" : "her");
		}
		else
		{
			requirements.Child.Popularity -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Oof, rejected. Thanks anyway, Grandpa. I thought that Columbian shrew mating dance you taught me was spot on. It could have gone worse, I guess.\n\n" +
				"{0}'s popularity down slightly.\n" +
				"Grandpa's insanity up slightly.\n" +
				"Grandpa's pride down slightly.",
				requirements.Child.Name);
		}

		return returnObj;
	}

	// Grandpa's Birthday
	public static Outcome Event1033(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(0, requirements.Parent.Popularity, (int)Enums.Difficulty.STANDARD) && 
			Constants.Roll(requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.STANDARD))
		{
			requirements.Parent.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa couldn't believe it when Mitt Romney jumped out of that cake! Everyone was there wishing Grandpa a happy birthday! " +
				"Even {2} was there kissing Grandpa's ring. Grandpa will treasure moments (and belongings) like these for the rest of his life.\n\n" +
				"{0}'s popularity up.\n" +
				"{1}'s popularity up.\n" +
				"Grandpa's pride way up.",
				requirements.Child.Name, requirements.Parent.Name, requirements.Grandpa.Name);
		}
		else if (Constants.Roll(0, requirements.Parent.Popularity, (int)Enums.Difficulty.VERY_EASY) && 
			Constants.Roll(requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.VERY_EASY))
		{
			requirements.Parent.Popularity += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
			requirements.Child.Popularity += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;
			requirements.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"The party was going pretty good until {2} released that swarm of hornets into the house. He shouted something about pride and sprinted away. " +
				"I didn't realize old guys could move that fast.\n\n" +
				"{0}'s popularity up slightly.\n" +
				"{1}'s popularity up slightly.\n" +
				"{2}'s pride up.\n" +
				"Grandpa's pride down slightly.",
				requirements.Child.Name, requirements.Parent.Name, requirements.Grandpa.Name);
		}
		else
		{
			requirements.Parent.Popularity -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
			requirements.Child.Popularity -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"That was... awkward. A lot of standing around. I hate it when people don't dance. Who invited all those nerds? Grandpa's gone and locked himself " +
				"in his room. \n\n" +
				"{0}'s popularity down slightly.\n" +
				"{1}'s popularity down slightly.\n" +
				"Grandpa's pride down.",
				requirements.Child.Name, requirements.Parent.Name);
		}

		return returnObj;
	}

	// Arbor Day
	public static Outcome Event1034(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll(requirements.Child.Cuteness, manager.PlayerFamily.Grandpa.Wisdom, (int)Enums.Difficulty.STANDARD))
		{
			manager.PlayerFamily.Grandpa.AddQualification (Qualification.GetQualificationByString ("ARBOR_DAY_SUCCESS"));
		}
		else
		{
			manager.PlayerFamily.Grandpa.AddQualification (Qualification.GetQualificationByString ("ARBOR_DAY_FAILURE"));
		}

		manager.Calendar.ScheduleEventInXDays(EventManager.GetEventById(1035), 55);

		requirements.Child.Intelligence += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

		manager.PlayerFamily.Grandpa.Pride += Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

		returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
		returnObj.OutcomeDescription = String.Format (
			"So you see, {0}, all you need is a little love and tenderness and a metric shitload of manure and even you can grow a beautiful oak! Let's come back here in " +
			"two months to see how the tree is coming along!\n\n" +
			"{0}'s intelligence up slightly.\n" +
			"Grandpa's pride up slightly.",
			requirements.Child.Name);
		
		return returnObj;
	}

	// Arbor Day Result
	public static Outcome Event1035(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (manager.PlayerFamily.Grandpa.Qualifications.Contains (Qualification.GetQualificationByString ("ARBOR_DAY_SUCCESS"))) {
			manager.PlayerFamily.Grandpa.RemoveQualification (Qualification.GetQualificationByString ("ARBOR_DAY_SUCCESS"));

			manager.PlayerFamily.Grandpa.Wisdom += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"Against all the laws of nature and science, a mere two months after Arbor Day, you and Grandpa are at the base of a towering oak staring at its leafy boughs waving wistfully " +
				"in the summer breeze. For maybe the first time in his life, Grandpa seems speechless. \n\n" +
				"Grandpa's wisdom up.\n" +
				"Grandpa's pride up.");
		} 
		else 
		{
			manager.PlayerFamily.Grandpa.RemoveQualification (Qualification.GetQualificationByString ("ARBOR_DAY_FAILURE"));

			manager.PlayerFamily.Grandpa.Wisdom -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"The tree Grandpa planted is a shiveled husk. I've never seen a deader-looking tree in all my years on this earth. And here I thought Grandpa had a green thumb...\n\n" +
				"Grandpa's wisdom down.\n" +
				"Grandpa's pride down slightly.");
		}

		return returnObj;
	}

	// Fourth of July
	public static Outcome Event1036(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		requirements.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

		manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

		returnObj.Mail = new Mail();
		returnObj.Mail.Date = manager.Calendar.GetCurrentDay();
		returnObj.Mail.Subject = "The Display Last Night";
		returnObj.Mail.Sender = requirements.Grandpa.Name;
		returnObj.Mail.Message = string.Format(
			"Grandpa {1},\n\n\t" +
			"That was quite a display you lit up outside my house last night. Really cool, man. My son had an important meeting today, but he fell asleep halfway through it. Now he's " +
			"fired and our house might be foreclosed on. I hope you know that you've made a powerful enemy here today. You haven't seen the last of me.\n\nFuck you,\n{0}", 
			requirements.Grandpa.Name, manager.PlayerFamily.Grandpa.Name);

		returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
		returnObj.OutcomeDescription = String.Format (
			"That was quite a fireworks display! Right outside {0}'s house too. For two hours. At 3 AM.\n\n" +
			"{0}'s pride down.\n" +
			"Grandpa's pride up.",
			requirements.Grandpa.Name);

		return returnObj;
	}

	// Grandpa's gone fishing
	public static Outcome Event1037(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll (requirements.Child.Cuteness, manager.PlayerFamily.Grandpa.Wisdom, (int)Enums.Difficulty.STANDARD)) 
		{	
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.Intelligence += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			returnObj.Mail = new Mail ();
			returnObj.Mail.Date = manager.Calendar.GetCurrentDay ();
			returnObj.Mail.Subject = "Cancer Cure";
			returnObj.Mail.Sender = "Steve Einstein";
			returnObj.Mail.Message = string.Format (
				"Grandpa {0},\n\n\t" +
				"Let me be the first to tell you that you are being put forward as a potential nobel prize of medicine candidate. The fish you caught yesterday showed us a gene that... " +
				"Well, it's some fancy science stuff that I'm not sure you'll understand but we have cured all cancer! Thanks to you!\n\nThank you!!!\nSteve Einstein", 
				manager.PlayerFamily.Grandpa.Name);

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Holy cow Grandpa! That was the biggest fish I've ever seen. I couldn't believe when those park rangers rushed over to take pictures and asked to study it for research " +
				"purposes. They say that fish could teach us to cure cancer. It's a miracle!\n\n" +
				"{0}'s popularity up.\n" +
				"{0}'s intelligence up slightly.\n" +
				"Grandpa's pride up.",
				requirements.Child.Name);
		} 
		else 
		{
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			requirements.Child.Intelligence -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			returnObj.Mail = new Mail ();
			returnObj.Mail.Date = manager.Calendar.GetCurrentDay ();
			returnObj.Mail.Subject = "Don't Do That";
			returnObj.Mail.Sender = "The EPA";
			returnObj.Mail.Message = string.Format (
				"Grandpa {0},\n\n\t" +
				"It has come to our attention that you poured several gallons of toxic waste into the lake. Please understand that we are very disappointed that you would choose to do that. " +
				"Hence this strongly worded letter. We would really rather prefer you did not do that again!\n\nSincerely yours,\nThe EPA", 
				manager.PlayerFamily.Grandpa.Name);

			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa, I don't think it was such a good idea to bring that barrel of toxic waste with us fishing. And when you started to fill the lake with it so all the dead fish " +
				"rose to the surface... It really felt like cheating. My head feels woozy from those fumes still...\n\n" +
				"{0}'s intelligence down.\n" +
				"Grandpa's pride down.",
				requirements.Child.Name);
		}
		return returnObj;
	}

	// April Fools
	public static Outcome Event1038(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		requirements.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

		manager.PlayerFamily.Grandpa.Insanity += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
		manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

		returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
		returnObj.OutcomeDescription = String.Format (
			"Ha! I took all the air out of {0}'s car's tires! Best prank ever! Then I rolled his car off a cliff! Got him!\n\n" +
			"{0}'s pride down.\n" +
			"Grandpa's insanity up slightly.\n" +
			"Grandpa's pride up.",
			requirements.Grandpa.Name);
		return returnObj;
	}


	// Parent promotion
	public static Outcome Event1039(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll (0, requirements.Parent.Intelligence, (int)Enums.Difficulty.EASY)) 
		{
			requirements.Parent.Intelligence += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Parent.Popularity += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_FOREVER;
			returnObj.OutcomeDescription = String.Format (
				"{0} has been putting in the long hours down at the toothpaste factory and it's finally paying off! {1} boss is promoting {2} " +
				"to Chief Regional Cap Screwer! What an honor.\n\n" +
				"{0}'s intelligence up.\n" +
				"{0}'s popularity up slightly.\n" +
				"Grandpa's pride up.",
				requirements.Parent.Name, Convert.ToBoolean (requirements.Parent.Gender) ? "Her" : "His", Convert.ToBoolean (requirements.Parent.Gender) ? "her" : "him");
		}
		else
			returnObj.Status = (int)Enums.EventOutcome.PASS;
		
		return returnObj;
	}

	// Parent catches coworker stealing
	public static Outcome Event1040(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (requirements.Accept) 
		{
			requirements.Parent.Popularity -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"\"STOP, THIEF,\" {0} yells! The thief stops dead in their tracks, pencils spilling out of their pocket. \"Wha-what?\" they mutter. But it's " +
				"too late. The work cops are already handcuffing the thief. They'll be going away for a long, long time.\n\n" +
				"{0}'s popularity way down!\n" +
				"Grandpa's pride up.",
				requirements.Parent.Name);
		}
		else
		{
			requirements.Parent.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"\"H-hey,\" {0} says as the thief turns to look at {1}. \"Cool stealing.\" {0} gives the thief a thumbs up, hands shaking. \"Wow, didn't realize you were " +
				"so cool, {0},\" the thief says. Nice!\n\n" +
				"{0}'s popularity up.",
				requirements.Parent.Name, Convert.ToBoolean (requirements.Parent.Gender) ? "her" : "him");
		}

		return returnObj;
	}

	// Grandpa sells drugs
	public static Outcome Event1041(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (requirements.Accept) 
		{
			requirements.Child.AddQualification (Qualification.GetQualificationByString ("GRANDPA_SELLING_DRUGS"));

			manager.PlayerFamily.Grandpa.MoneyGrowth += 200;
			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.InsanityGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Pleasure doing business with you! Grandpa rolls up another twenty and stuffs it in his back pocket along with the rest of them. Business is booming!\n\n" +
				"Grandpa's insanity up.\n" +
				"Grandpa's insanity growth up.\n" +
				"Grandpa's income up by 200 dollars a month.");
		}
		else
			returnObj.Status = (int)Enums.EventOutcome.PASS_BLACKLIST_YEAR;

		return returnObj;
	}

	// Child catches classmate doing drugs. 
	public static Outcome Event1042(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (requirements.Accept) 
		{
			requirements.Child.RemoveQualification (Qualification.GetQualificationByString ("GRANDPA_SELLING_DRUGS"));

			manager.PlayerFamily.Grandpa.MoneyGrowth -= 200;
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_FOREVER;
			returnObj.OutcomeDescription = String.Format (
				"\"Freeze, it's the FBI!\" Grandpa throws his hands up into the air. {0} catching those kids in the bathroom started a massive investigation which led right back " +
				"to Grandpa himself! Book him, boys!\n\n" +
				"Grandpa's pride way way down!!\n" +
				"Grandpa's income down by 200 dollars a month.",
				requirements.Child.Name);
		}
		else
		{
			requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"No need to ruffle any feathers! \n\n" +
				"{0}'s popularity up.\n" +
				"Grandpa's pride up slightly.",
				requirements.Child.Name);
		}

		return returnObj;
	}

	// Grandkid fights bully
	public static Outcome Event1043(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll (requirements.Child.Cuteness, requirements.Child.Athleticism, (int)Enums.Difficulty.STANDARD)) 
		{
			requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.Athleticism += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"{0} is being picked on by a bully at school. One day, {1} has simply had enough and challenges the bully to a showdown at high noon... " +
				"And {0} beats the ever-living shit out of them! {0} flawlessly dodges every punch the bully throws. The bully has to go to the hospital. Nice going, {0}!\n\n" +
				"{0}'s popularity up.\n" +
				"{0}'s athleticism up slightly.\n" +
				"Grandpa's pride up.",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "she" : "he");
		} 
		else if (Constants.Roll (requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.STANDARD)) 
		{
			requirements.Child.Popularity += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"{0} is being picked on by a bully at school. One day, {1} has simply had enough and challenges the bully to a showdown at high noon... " +
				"But before it starts, all the kids in {0}'s grade dogpile onto the bully beating the ever-living shit out of them! {0} is just that popular!\n\n" +
				"{0}'s popularity up slightly.\n" +
				"Grandpa's pride down slightly.",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "she" : "he");
		} 
		else 
		{
			requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"{0} is being picked on by a bully at school. One day, {1} has simply had enough and challenges the bully to a showdown at high noon... " +
				"And {0} gets the ever-living shit beaten out of {2}! So sorry to have said anything, Mr. Bully.\n\n" +
				"{0}'s popularity down.\n" +
				"Grandpa's pride down.",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "she" : "he", Convert.ToBoolean(requirements.Child.Gender) ? "her" : "him");
		}

		return returnObj;
	}

	// Child takes test
	public static Outcome Event1044(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll (requirements.Child.Cuteness, requirements.Child.Intelligence, (int)Enums.Difficulty.HARD)) 
		{
			requirements.Child.Intelligence += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"{0} had a test today on {1}... And aced it! The teacher has never seen a score that high. I didn't know there was a percentage above 100! Congrats!\n\n" +
				"{0}'s intelligence up.\n" +
				"Grandpa's pride up.",
				requirements.Child.Name, Convert.ToBoolean(Constants.RANDOM.Next(0, 1)) ? "History of Asian Fishing" : "Mega Geometry");
		}
		else
		{
			requirements.Child.Intelligence -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"{0} had a test today on {1}... And completely bombed it! {2} even stayed up late studying... The teacher is making {3} sit in the loser chair for the rest of the term...\n\n" +
				"{0}'s intelligence down.\n" +
				"Grandpa's pride down.",
				requirements.Child.Name, Convert.ToBoolean(Constants.RANDOM.Next(0, 1)) ? "Conversational Latin" : "History of Salad", Convert.ToBoolean(requirements.Child.Gender) ? "She" : "He", Convert.ToBoolean(requirements.Child.Gender) ? "her" : "him");
		}

		return returnObj;
	}


	// Grandkid's class does fingerpainting
	public static Outcome Event1045(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.EASY) && 
			Constants.Roll(requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.STANDARD))
   		{
			requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"{0}'s class was fingerpainting today. {0} did a very good job sharing {1} paints. On top of that, {2} painted an exact replica of the Mona Lisa! Cool! The fat cats in the pricinpal's office are calling it " +
				"a \"miracle\"! Ha!\n\n" +
				"{0}'s artistry up.\n" +
				"{0}'s popularity up.\n" +
				"Grandpa's pride up.",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "her" : "his", Convert.ToBoolean(requirements.Child.Gender) ? "she" : "he");
   		}
		else if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.STANDARD))
   		{
			requirements.Child.Artistry += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
			requirements.Child.Popularity += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"{0}'s class was fingerpainting today. {0} spilled paint all over the page and {1}... But it came out looking exactly like Van Gough's Starry Night. Kind of freaky actually. {2} " +
				"kindergarten teacher apparently refuses to teach {3} now... Something about demons." +
				"a \"miracle\"! Ha!\n\n" +
				"{0}'s artistry up slightly.\n" +
				"{0}'s popularity up slightly.\n" +
				"Grandpa's pride up slightly.",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "herself" : "himself", 
				Convert.ToBoolean(requirements.Child.Gender) ? "Her" : "His", Convert.ToBoolean(requirements.Child.Gender) ? "her" : "him");
   		}
		else
		{	
			requirements.Child.Artistry -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"{0}'s class was fingerpainting today... But {0} couldn't be roused from nap time. {2} just kept sleeping and couldn't be woken up. They actually thought {1} was dead. This is no way for a young child to " +
				"build finger painting skills for the real world!\n\n" +
				"{0}'s artistry down.\n" +
				"Grandpa's pride down.",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "she" : "he", Convert.ToBoolean(requirements.Child.Gender) ? "She" : "He");
		}

		return returnObj;
	}

	// Grandkid's school dance
	public static Outcome Event1046(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll (requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.VERY_HARD)) 
		{
			requirements.Child.Popularity += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			requirements.Child.PopularityGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Money += 80;
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"Nice dancing, {0}! I think you danced with every {1} in the building. And they were still lining up. I couldn't believe when you started charging them " +
				"per dance. That's economical!\n\n" +
				"{0}'s popularity way up!\n" +
				"{0}'s popularity growth up.\n" +
				"Grandpa gained 80 dollars.\n" +
				"Grandpa's pride way up!",
				requirements.Child.Name, Convert.ToBoolean (requirements.Child.Gender) ? "guy" : "girl");
		} 
		else if (Constants.Roll (requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.EASY)) 
		{	
			requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.PopularityGrowth += Constants.Character.MINOR_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"That went adequately! {0} may have gotten shot down by every {1} {2} asked to dance, but at least {2} tried. That's worth something, right? Wait, it isn't? " +
				"Nevermind, then, that was a waste of time.\n\n" +
				"{0}'s popularity up.\n" +
				"{0}'s popularity growth up slightly.\n" +
				"Grandpa's pride up.",
				requirements.Child.Name, Convert.ToBoolean (requirements.Child.Gender) ? "guy" : "girl", Convert.ToBoolean (requirements.Child.Gender) ? "she" : "he");
		} 
		else 
		{
			requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"Talk about a wallflower. Standing by the punch table and avoiding eye contact with anyone who even attempted to make eye contact with you between fistfuls of food? " +
				"Not exactly graceful, {0}.\n\n" +
				"{0}'s popularity down.\n" +
				"Grandpa's pride down.",
				requirements.Child.Name, Convert.ToBoolean (requirements.Child.Gender) ? "guy" : "girl");
		}

		manager.Calendar.ScheduleEventInXDays(EventManager.GetEventById(1046), 99);
		return returnObj;
	}

	// Grandpa goes for a walk in the park.
	public static Outcome Event1047(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();
		if (Constants.Roll (0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.STANDARD) &&
			requirements.Child.Age < 10 && manager.PlayerFamily.Children.Count > 1) 
		{
			SimulationEvent ev = GetEventById (1048);
			ev.Requirements.Child = requirements.Child;
			manager.PlayerFamily.Children.Remove (requirements.Child);
			manager.Calendar.ScheduleEventInXDays(ev, 7);

			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa, I thought {0} went with you? What do you mean you don't think so? Where is {1}?! Lost in the woods???\n\n" +
				"{0} left the family!\n" +
				"Grandpa's insanity up.\n" +
				"Grandpa's pride way way down!!",
				requirements.Child.Name, Convert.ToBoolean (requirements.Child.Gender) ? "she" : "he");
		} 
		else if (Constants.Roll (requirements.Child.Cuteness, manager.PlayerFamily.Grandpa.Wisdom, (int)Enums.Difficulty.STANDARD)) 
		{	
			requirements.Child.Intelligence += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Wisdom += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa regales you with tales of a time when he was a wee boy. Grandpa once socked Hitler in the jaw. It's true. Open a history book, you " +
				"neanderthal.\n\n" +
				"{0}'s intelligence up.\n" +
				"Grandpa's wisdom up.\n" +
				"Grandpa's pride up.",
				requirements.Child.Name);
		} 
		else 
		{
			requirements.Child.Athleticism += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"You and Grandpa have a nice time at the park. You toss around a frisbee a little. There's a slight chill in the air.\n\n" +
				"{0}'s athleticism up slightly.\n" +
				"Grandpa's pride up slightly.",
				requirements.Child.Name);
		}
	
		return returnObj;
	}

	// Found child
	public static Outcome Event1048(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		requirements.Child.Intelligence += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
		requirements.Child.Athleticism += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

		manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

		manager.PlayerFamily.Children.Add (requirements.Child);

		returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
		returnObj.OutcomeDescription = String.Format (
			"After a week of fearful searching, {0} stumbles out of the underbrush one day covered in poison ivy and missing a toe. {2} has seen some shit. Grandpa is " +
			"somewhat relieved to see {0} alive! After all, {1} had some very important stats for the family team!\n\n" +
			"{0} returned to the family!\n" +
			"{0}'s intelligence up.\n" +
			"{0}'s athleticism up.\n" +
			"Grandpa's pride up.",
			requirements.Child.Name, Convert.ToBoolean (requirements.Child.Gender) ? "she" : "he", Convert.ToBoolean (requirements.Child.Gender) ? "She" : "He");

		return returnObj;
	}

	// Child joins scouts
	public static Outcome Event1049(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (requirements.Accept) 
		{
			requirements.Child.AddQualification (Qualification.GetQualificationByString ("IN_SCOUTS"));

			requirements.Child.Athleticism += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.AthleticismGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;
			requirements.Child.Popularity += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"Of course you can join the scouts, {0}! Think of all the skills you'll build that can then be leveraged for skill checks! {0} is overjoyed!\n\n" +
				"{0}'s athleticism up.\n" +
				"{0}'s athleticism growth up.\n" +
				"Grandpa's pride down slightly.",
				requirements.Child.Name);
		} 
		else 
		{
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"No no no! You will not be tying knots in the woods like an animal! Those skills are not transferrable! {0} runs to their room crying. It's the for the best. " +
				"Probably.\n\n" +
				"Grandpa's pride up.",
				requirements.Child.Name, Convert.ToBoolean (requirements.Child.Gender) ? "she" : "he");
		}
			
		return returnObj;
	}

	// Student council election rigging?
	public static Outcome Event1050(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (requirements.Accept) 
		{
			manager.PlayerFamily.Grandpa.AddQualification (Qualification.GetQualificationByString ("RIGGED_ELECTION"));

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"Wonderful. Everything is all set. Just had to grease a few palms with favors. Next week when the votes are counted, My grandchild will be the winner by a tight margin.");
		} 
		else 
		{
			manager.PlayerFamily.Grandpa.Wisdom += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"No! I will not sully this holy process with lies!\n\n" +
				"Grandpa's wisdom up.\n" +
				"Grandpa's pride up.");
		}

		return returnObj;
	}

	// Student council election
	public static Outcome Event1051(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		// Caught rigging
		if (manager.PlayerFamily.Grandpa.Qualifications.Contains (Qualification.GetQualificationByString ("RIGGED_ELECTION")) && 
			Constants.RANDOM.Next(1,100) < 20) 
		{
			manager.Calendar.ScheduleEventInXDays(EventManager.GetEventById(1052), 10);
		}

		if (manager.PlayerFamily.Grandpa.Qualifications.Contains(Qualification.GetQualificationByString ("RIGGED_ELECTION")) ||
			Constants.Roll (requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.HARD)) 
		{
            manager.PlayerFamily.Grandpa.RemoveQualification (Qualification.GetQualificationByString ("RIGGED_ELECTION"));
			requirements.Child.AddQualification (Qualification.GetQualificationByString ("STUDENT_COUNCIL"));

			requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.PopularityGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"{0} has joined the pantheon of great presidents such as George Washington, Ben Franklin, and Charlemagne! May {1} rule be long and prosperous!\n\n" +
				"{0}'s popularity up.\n" +
				"{0}'s popularity growth up.\n" +
				"Grandpa's pride way up!",
				requirements.Child.Name, Convert.ToBoolean (requirements.Child.Gender) ? "her" : "his");
		} 
		else 
		{
			requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"Too bad, {0}. I guess no one at school actually likes you. Maybe the big X's drawn on all your campaign posters should have been a hint. There's always next " +
				"year, though.\n\n" +
				"{0}'s popularity down.\n" +
				"Grandpa's pride down.",
				requirements.Child.Name);
		}

		return returnObj;
	}
		
	// Caught rigging
	public static Outcome Event1052(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		requirements.Child.RemoveQualification (Qualification.GetQualificationByString ("STUDENT_COUNCIL"));

		requirements.Child.Popularity -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
		requirements.Child.PopularityGrowth -= Constants.Character.MAJOR_STAT_GROWTH_AMOUNT;

		manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT * 2;

		returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
		returnObj.OutcomeDescription = String.Format (
			"{0} knew there was something fishy about those elections. That is why he worked tirelessly day and night to solve the mystery. Going through the ballots, though, one thing didn't add " +
			"up. Why did someone write \"RIGGED BALLOT\" on three hundred ballots? Ah ha! Checkmate! {1} looks at Grandpa... \"How could you?!\" {1}'s student council membership is taken away.\n\n" +
			"{1}'s popularity way down!\n" +
			"{1}'s popularity growth way down!\n" +
			"Grandpa's pride way way way way down!!!!",
			requirements.Grandpa.Name, requirements.Child.Name);

		return returnObj;
	}

	// Take your grandpa to work day
	public static Outcome Event1053(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll (0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.STANDARD) && 
			!Constants.Roll (0, requirements.Parent.Intelligence, (int)Enums.Difficulty.EASY)) 
		{
			requirements.Parent.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Parent.Love -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Parent.PopularityGrowth -= Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"\"No, Grandpa! Don't touch that!\" But it's too late. Grandpa has touched it. Red lights start flashing all throughout the facility. Fires sprout up everywhere. " +
				"Wow who made a button with this functionality?! Anyway, looks like {0} is out of a job.\n\n" +
				"{0}'s popularity down.\n" +
				"{0}'s love down.\n" +
				"{0}'s popularity growth down.\n" +
				"Grandpa's pride down.",
				requirements.Parent.Name);
		} 
		else 
		{
			requirements.Parent.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Parent.Love += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Parent.PopularityGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"\"Oh, Grandpa, you're so funny!\" Grandpa is a big hit at work. He teaches {0}'s boss how to be a better boss as well! Productivity is up 70%!\n\n" +
				"{0}'s popularity up.\n" +
				"{0}'s love up.\n" +
				"{0}'s popularity growth up.\n" +
				"Grandpa's pride up.",
				requirements.Parent.Name);
		}

		return returnObj;
	}

	// Grandpa goes to the bar
	public static Outcome Event1054(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll (0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.HARD) ||
			Constants.RANDOM.Next(1,100) < 50) 
		{
			requirements.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"\"{0}, what did you say about my grandson?!\" Grandpa socks {0} right in the mouth. {0} slams a chair over Grandpa's head. BAR FIGHT!! Everyone starts breaking windows and " +
				"glasses and causing chaos. The cops are called. There are no real consequences like usual.\n\n" +
				"{0}'s pride down.\n" +
				"Grandpa's pride down.",
				requirements.Grandpa.Name);
		} 
		else 
		{
			requirements.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"Against all odds, Grandpa has a relaxing night with {0}. Trading war stories and grandkid stories. They have more in common than they originally thought. They're just " +
				"combatants on opposite sides of a conflict. But the conflict must go on. All nice nights must end.\n\n" +
				"{0}'s pride up.\n" +
				"Grandpa's pride up.",
				requirements.Grandpa.Name);
		}

		return returnObj;
	}

	// Field Trip
	public static Outcome Event1055(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll (requirements.Child.Cuteness, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.HARD) ||
			(! Constants.Roll (requirements.Child.Cuteness, requirements.Parent.Love, (int)Enums.Difficulty.EASY))) 
		{
			requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			requirements.Parent.Love -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Parent.LoveGrowth -= Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"From the second they get on the bus, Grandpa, {0}, and {1} are fighting non stop. All the other kids feel very awkward. At the farm, Grandpa kicks a hole through every " +
				"pumpkin he can find. {0} just drinks the whole time. {1} cries on the bus ride home.\n\n" +
				"{0}'s love down.\n" +
				"{0}'s love growth down.\n" +
				"{1}'s popularity down.\n" +
				"Grandpa's insanity up.\n" +
				"Grandpa's pride down.",
				requirements.Parent.Name, requirements.Child.Name);
		} 
		else 
		{
			requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			requirements.Parent.Love += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"That was surprisingly fun! Grandpa picked out a big pumpkin, and everyone ate apple cider donuts.\n\n" +
				"{0}'s love up.\n" +
				"{1}'s popularity up.\n" +
				"Grandpa's pride up.",
				requirements.Parent.Name, requirements.Child.Name);
		}

		return returnObj;
	}

	// Grandkid takes joyride
	public static Outcome Event1056(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (!Constants.Roll (requirements.Child.Cuteness, requirements.Child.Intelligence, (int)Enums.Difficulty.VERY_EASY) && manager.PlayerFamily.Children.Count > 1)
		{
			foreach (Parent parent in manager.PlayerFamily.Parents) 
			{
				parent.Love -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			}

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			manager.PlayerFamily.Children.Remove (requirements.Child);

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"{0} goes for a joyride with {1}'s car. However, {0} keeps checking their Snapchat, and, unfortunately, the car veers into an " +
				"oncoming semi-truck. {0} perishes in a fiery explosion.\n\n" +
				"{0} has left the family!!\n" +
				"All parents' love way down!!\n" +
				"Grandpa's pride down.",
				requirements.Child.Name, requirements.Parent.Name);
		} 
		else if (!Constants.Roll (requirements.Child.Cuteness, requirements.Child.Intelligence, (int)Enums.Difficulty.STANDARD))
		{
			requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.PopularityGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			requirements.Child.Intelligence -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"Woo hoo! {0} just went on a wild joyride with {1}'s car! Fortunately, {0} kept their eyes and ears on the road so no one was hurt! " +
				"That could have gone terribly!\n\n" +
				"{0}'s popularity up.\n" +
				"{0}'s popularity growth up.\n" +
				"{0}'s intelligence down.\n" +
				"Grandpa's pride up.",
				requirements.Child.Name, requirements.Parent.Name);
		} 
		else
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Veteran's Day
	public static Outcome Event1057(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (!Constants.Roll (0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.STANDARD))
		{
			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format (
				"No, Grandpa, no! Put the knife down! Grandpa stumbled onto a civil war reenactment and was reminded of his days in 'nam. He seems to be having " +
				"a fit. Poor Grandpa.\n\n" +
				"Grandpa's insanity up.\n" +
				"Grandpa's pride down.");
		} 
		else
		{
			manager.PlayerFamily.Grandpa.Wisdom += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa lounges peacefully at the local park watching the Civil War reenactment. He reminisces about his own service to our blessed country " +
				"and the friends he made and lost there.\n\n" +
				"Grandpa's wisdom up.\n" +
				"Grandpa's pride way up!");
		} 

		return returnObj;
	}

    // Thanksgiving
    public static Outcome Event1058(DataManager manager, Requirement requirements)
    {
        Outcome returnObj = new Outcome();

        if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Wisdom, (int)Enums.Difficulty.STANDARD))
        {
            foreach (Parent parent in manager.PlayerFamily.Parents)
            {
                parent.Love += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            }

            manager.PlayerFamily.Grandpa.Wisdom += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

            returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
            returnObj.OutcomeDescription = String.Format(
                "Grandpa put on his chef's hat and told the parents to sit back and drink a beer this holiday. He'd be charge of the kitchen! And what a kitchen " +
                "he ran! Absolutely delicious dishes the likes of which the family has never seen! Amazing job, Grandpa!\n\n" +
                "All parents' love up!\n" +
                "Grandpa's wisdom up.\n" +
                "Grandpa's pride up.");
        }
        else
        {
            manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

            returnObj.Status = (int)Enums.EventOutcome.FAILURE;
            returnObj.OutcomeDescription = String.Format(
                "It's Thanksgiving! Time to give thanks for what you have! Like family. The family that Grandpa has mercilessly torn apart lately. He offered to " +
                "cook everyone dinner tonight, but instead he just got drunk and fell asleep in the garage. We are all sharing a single can of beans.\n\n" +
                "Grandpa's insanity up.\n" +
                "Grandpa's pride down.");
        }

        return returnObj;
    }

	// School Band Tryouts
	public static Outcome Event1059(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if ((Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.STANDARD) &&
			Constants.Roll(requirements.Child.Cuteness, requirements.Child.Intelligence, (int)Enums.Difficulty.STANDARD)) 
			||
			Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.VERY_HARD))
		{
			requirements.Child.AddQualification (Qualification.GetQualificationByString ("IN_SCHOOL_BAND"));

			requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.ArtistryGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;
			requirements.Child.Intelligence += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT * 2;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"{0} played a beautiful sonata. Incredible. It was so good people were collapsing to their knees having religious revelations. {0} is going to be " +
				"first chair in band!\n\n" +
				"{0}'s artistry up.\n" +
				"{0}'s artistry growth up.\n" +
				"{0}'s intelligence up.\n" +
				"Grandpa's pride way up!",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "her" : "his");
		}
		else if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.EASY))
		{
			requirements.Child.AddQualification (Qualification.GetQualificationByString ("IN_SCHOOL_BAND"));

			requirements.Child.Artistry += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
			requirements.Child.ArtistryGrowth += Constants.Character.MINOR_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"{0} played Hot-Crossed Buns three times in a row. The judges were not super impressed, but they're lacking for band members. {0} will be last chair! Nice! \n\n" +
				"{0}'s artistry up slightly.\n" +
				"{0}'s artistry growth up slightly.\n" +
				"Grandpa's pride up slightly.",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "her" : "his");
		}
		else
		{
			requirements.Child.Artistry -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
			requirements.Child.ArtistryGrowth -= Constants.Character.MINOR_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format(
				"People were covering their ears and screaming. {0}, how could you do such horrible things to a tuba?\n\n" +
				"{0}'s artistry down slightly.\n" +
				"{0}'s artistry growth dpwm slightly.\n" +
				"Grandpa's pride down.",
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "her" : "his");
		}	

		return returnObj;
	}

	// Dating Supermodel
	public static Outcome Event1060(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll(0, requirements.Parent.Popularity, (int)Enums.Difficulty.HARD))
		{
			requirements.Parent.Popularity += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			requirements.Parent.Love += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			requirements.Parent.LoveGrowth += Constants.Character.MAJOR_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_FOREVER;
			returnObj.OutcomeDescription = String.Format(
				"{0} has been spending a lot of time living it up after the divorce. But just last week, {1} started seeing the world-famous supermodel " +
				"ROBIN SPEILHOFFER! That has to be an ego boost after a tough divorce! \n\n" +
				"{0} popularity way up!\n" +
				"{0} love way up!\n" +
				"{0} love growth way up!\n" +
				"Grandpa's pride way up!.",
				requirements.Parent.Name, Convert.ToBoolean(requirements.Parent.Gender) ? "she" : "he");
		}
		else
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Grandpa threatens president
	public static Outcome Event1061(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.STANDARD))
		{
			foreach (Parent parent in manager.PlayerFamily.Parents) 
			{
				parent.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			}

			foreach (Child child in manager.PlayerFamily.Children) 
			{
				child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			}

			manager.PlayerFamily.Grandpa.Insanity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format(
				"\"And you'll never be half the president Reagan was!\" Grandpa, no! Grandpa just got off the phone with the president after violently " +
				"threatening him! The family is much more popular due to their 15 minutes of fame!\n\n" +
				"All parents' popularity up!\n" +
				"All children's popularity up!\n" +
				"Grandpa's insanity up.\n" +
				"Grandpa's pride down.");
		}
		else
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Grandkid is a chef
	public static Outcome Event1062(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.STANDARD))
		{
			requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
			requirements.Child.ArtistryGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"Wow, {0}, that was the best {1} I've ever had! You're turning into quite the amateur chef! Maybe you should go on " +
				"one of those cooking shows?\n\n" +
				"{0}'s artistry up.\n" +
				"{0}'s artistry growth up.\n" +
				"Grandpa's pride up.",
				requirements.Child.Name, Convert.ToBoolean(Constants.RANDOM.Next(0, 1)) ? "Moon Steak" : "Endangered Fish Ravioli");
		}
		else
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Parent gets raise
	public static Outcome Event1063(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll(0, requirements.Parent.Intelligence, (int)Enums.Difficulty.STANDARD) ||
			Constants.Roll(0, requirements.Parent.Popularity, (int)Enums.Difficulty.VERY_HARD))
		{
			requirements.Parent.Intelligence += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
			int raise_amount = Constants.RANDOM.Next(170, 280);
			manager.PlayerFamily.Grandpa.MoneyGrowth += raise_amount;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"{0}'s boss claps them on the back. \"I've been hearing some good things about you. Some very, very good things. I think it's about " +
				"time for a promotion!\" The whole family is ecstatic. Grandpa slips a few bills from {0}'s wallet when {1} isn't looking.\n\n" +
				"{0}'s intelligence up.\n" +
				"Grandpa's income up by {2} per month.\n" +
				"Grandpa's pride up.",
				requirements.Parent.Name, Convert.ToBoolean (requirements.Parent.Gender) ? "she" : "he", raise_amount.ToString());
		}
		else
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}
		
	// Parents adopt an orphan
	public static Outcome Event1064(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		Child new_child = manager.Orphanage.GetRandomEligibleChild (0, 1000);

		if (Constants.Roll(0, requirements.Parent.Love, (int)Enums.Difficulty.VERY_HARD) && new_child != null)
		{
			manager.Orphanage.Children.Remove (new_child);
			manager.PlayerFamily.Children.Add (new_child);

			foreach (Parent parent in manager.PlayerFamily.Parents) 
			{
				parent.Love += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
				parent.Popularity += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			}

			foreach (Child child in manager.PlayerFamily.Children) 
			{
				child.Popularity += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			}

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format (
				"{0} has decided that the time is right to adopt an orphan! Please welcome {1} to the family! The entire family is overjoyed, especially {1}. {1} " +
				"is so happy {2} gets to be in a normal, well-adjusted family for once.\n\n" +
				"{1} has joined the family!\n" +
				"All parents' love way up!!\n" +
				"The entire family's popularity way up!!\n" +
				"Grandpa's pride way way up!!",
				requirements.Parent.Name, new_child.Name, Convert.ToBoolean (new_child.Gender) ? "she" : "he");
		}
		else
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Parent marries a stripper
	public static Outcome Event1065(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		Parent new_parent = manager.Orphanage.GetRandomParent ();

		if (Constants.Roll(0, requirements.Parent.Love, (int)Enums.Difficulty.EASY) && requirements.Money >= 100 && new_parent != null) 
		{
			requirements.Parent.RemoveQualification (Qualification.GetQualificationByString ("IS_SINGLE"));

			requirements.Parent.Love += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
			new_parent.Love += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;

			manager.Orphanage.Parents.Remove (new_parent);
			manager.PlayerFamily.Parents.Add (new_parent);

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_FOREVER;
			returnObj.OutcomeDescription = String.Format (
				"{0} stumbles home extremely drunk with someone you don't recognize on their arm. \"Kids, this is {1}! Your new, hic, {2}!\" " +
				"Oh, {0} has married a stripper. Alright, then.\n\n" +
				"{1} has joined the family!\n" +
				"{0}'s love way up!\n" +
				"{1}'s love way up!\n" +
				"Grandpa's pride way way up!!",
				requirements.Parent.Name, new_parent.Name, Convert.ToBoolean (new_parent.Gender) ? "mommy" : "daddy");
		}
		else
		{
			requirements.Parent.Love -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Well that was weird. And not good weird. Kids, don't go to the strip club with your Grandpa.\n\n" +
				"{0}'s love down.\n" +
				"Grandpa's pride up.",
				requirements.Parent.Name);
		}

		return returnObj;
	}

	// Grandpa gets insurance fraud payout
	public static Outcome Event1066(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Wisdom, (int)Enums.Difficulty.EASY))
		{
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.Money += 800;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa gets a payout from his old insurance fraud scheme. Those were the days.\n\n" +
				"Grandpa gains $800.\n" +
				"Grandpa's pride up slightly.");
		}
		else
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

	// Grandpa sells fingernails
	public static Outcome Event1067(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Wisdom, (int)Enums.Difficulty.EASY))
		{
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;
			manager.PlayerFamily.Grandpa.MoneyGrowth += 200;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_FOREVER;
			returnObj.OutcomeDescription = String.Format (
				"Grandpa gets some starling news from his doctor. Apparently his toenails are laced with uranium. Frankly, it's a medical mystery. Anyway, the doctors " +
				"are willing to pay him an additional 200 dollars a month to stop by.\n\n" +
				"Grandpa gains $200 per month.\n" +
				"Grandpa's pride up slightly.");
		}
		else
			returnObj.Status = (int)Enums.EventOutcome.PASS;

		return returnObj;
	}

    //finals seeding
    public static Outcome Event3001(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();

		manager.LeagueFamilies = manager.LeagueFamilies.OrderBy(o => o.Grandpa.Pride).ToList();

		manager.Calendar.ScheduleEventInXDays(EventManager.GetEventById(3), 1);


        ret.Status = (int)Enums.EventOutcome.SUCCESS;
        
		// QUARTERFINALS
		SimulationEvent quarterFinal1 = EventManager.GetEventById(3002);
		quarterFinal1.Requirements.Grandpa = manager.LeagueFamilies[0].Grandpa;

		SimulationEvent quarterFinal2 = EventManager.GetEventById(3003);
		quarterFinal2.Requirements.Grandpa = manager.LeagueFamilies[0].Grandpa;

		SimulationEvent quarterFinal3 = EventManager.GetEventById(3004);
		quarterFinal3.Requirements.Grandpa = manager.LeagueFamilies[0].Grandpa;

		SimulationEvent quarterFinalJudge = EventManager.GetEventById(3005);
		quarterFinalJudge.Requirements.Grandpa = manager.LeagueFamilies[0].Grandpa;

		// SEMIFINALS
		SimulationEvent semiFinal1 = EventManager.GetEventById(3006);
		semiFinal1.Requirements.Grandpa = manager.LeagueFamilies[manager.LeagueFamilies.Count - 2].Grandpa;

		SimulationEvent semiFinal2 = EventManager.GetEventById(3007);
		semiFinal2.Requirements.Grandpa = manager.LeagueFamilies[manager.LeagueFamilies.Count - 2].Grandpa;

		SimulationEvent semiFinal3 = EventManager.GetEventById(3008);
		semiFinal3.Requirements.Grandpa = manager.LeagueFamilies[manager.LeagueFamilies.Count - 2].Grandpa;

		SimulationEvent semiFinalJudge = EventManager.GetEventById(3009);
		semiFinalJudge.Requirements.Grandpa = manager.LeagueFamilies[manager.LeagueFamilies.Count - 2].Grandpa;

		// GRAND FINALS
		SimulationEvent grandFinal1 = EventManager.GetEventById(3010);
		grandFinal1.Requirements.Grandpa = manager.LeagueFamilies[manager.LeagueFamilies.Count - 1].Grandpa;

		SimulationEvent grandFinal2 = EventManager.GetEventById(3011);
		grandFinal2.Requirements.Grandpa = manager.LeagueFamilies[manager.LeagueFamilies.Count - 1].Grandpa;

		SimulationEvent grandFinal3 = EventManager.GetEventById(3012);
		grandFinal3.Requirements.Grandpa = manager.LeagueFamilies[manager.LeagueFamilies.Count - 1].Grandpa;

		SimulationEvent grandFinalJudge = EventManager.GetEventById(3013);
		grandFinalJudge.Requirements.Grandpa = manager.LeagueFamilies[manager.LeagueFamilies.Count - 1].Grandpa;

        ret.OutcomeDescription = string.Format(
			"So {0} wanted us to all go to Christmas Family Bonding Camp together as a family. Some kind of aptly-named bonding camp. Saying something about making memories " +
			"that we would always remember, blah blah blah. Not interested. What I am interested in, however, is that every member of the Grandpa League " +
			"will be there. Time to have this year's Christmas Family Bonding Camp final showdown. \n{1} and I are facing off first one week from today. Here goes!", 
			requirements.Parent.Name, manager.LeagueFamilies[0].Grandpa.Name);

        ret.Mail = new Mail();
		ret.Mail.Sender = manager.LeagueFamilies[0].Grandpa.Name;
        ret.Mail.Date = manager.Calendar.GetCurrentDay();
        ret.Mail.Subject = "Quarterfinal Matchup";
        ret.Mail.Message = string.Format(
			"Grandpa {0},\n\n\t" +
			"H-hey, how's it going? I noticed we were up against each other in round one. I know my family may not be the smartest or the strongest or the best-looking, " +
			"but we sure have a lot of heart. So... That has to count for something, right, Grandpa {0}? M-may the best family win...\n\n" +
			"Cordially yours,\n" +
			"{1}", 
			manager.PlayerFamily.Grandpa.Name, manager.LeagueFamilies[0].Grandpa.Name);

        return ret;
    }

    //QUARTER FINAL MATCH 1
    public static Outcome Event3002(DataManager manager, Requirement requirements)
    {
		Outcome returnObj = new Outcome();

		// Set enemy family
		Family enemyFamily = null;
		foreach(Family enemy in manager.LeagueFamilies)
		{
			if (enemy.Grandpa.Name == requirements.Grandpa.Name) 
			{
				enemyFamily = enemy;
				break;
			}
		}
		List<Child> enemyChildren = enemyFamily.Children.OrderBy(o => o.Artistry).ToList();
		Child enemyChild = enemyChildren[enemyChildren.Count - 1];

		if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.STANDARD) && 
			enemyChild.Artistry < requirements.Child.Artistry)
		{
			manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("QUARTERFINAL_MATCH_1_WIN"));
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"The contest is {0} versus {1}. They're both painting up a storm. Paint is flying everywhere. Mediums are mixing with no regard for safety. " +
				"Did {1} just put a rusty sawblade onto theirs? Brilliant. Ultimately, {0}'s painting could be hung in The Louvre.\n\n" +
				"SUCCESS!",
				requirements.Child.Name, enemyChild.Name);
		}
		else
		{
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format(
				"The contest is {0} versus {1}. They're both painting up a storm. Paint is flying everywhere. Mediums are mixing with no regard for safety. " +
				"Did {1} just put a rusty sawblade onto theirs? Brilliant. Ultimately, it's hard to deny that {1}'s painting is the best. Damn it.\n\n" +
				"FAILURE!",
				requirements.Child.Name, enemyChild.Name);
		}

		return returnObj;
    }

	//QUARTER FINAL MATCH 2
	public static Outcome Event3003(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		// Set enemy family
		Family enemyFamily = null;
		foreach(Family enemy in manager.LeagueFamilies)
		{
			if (enemy.Grandpa.Name == requirements.Grandpa.Name) 
			{
				enemyFamily = enemy;
				break;
			}
		}
		List<Child> enemyChildren = enemyFamily.Children.OrderBy(o => o.Popularity).ToList();
		Child enemyChild = enemyChildren[enemyChildren.Count - 1];

		if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.STANDARD) && 
			enemyChild.Popularity < requirements.Child.Popularity)
		{
			manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("QUARTERFINAL_MATCH_2_WIN"));
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"{1} decides to host a hip 70's party. Those are cool, right? WRONG, {0} has a huge blowout 90's night. Everyone comes. " +
				"It's not even close.\n\n" +
				"SUCCESS!",
				requirements.Child.Name, enemyChild.Name);
		}
		else
		{
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format(
				"Well, at first it seemed like no one was going to show up to either kid's party... But finally, around 10pm, one person comes... \"Hey, I'm looking " +
				"for {1}'s less lame looking party?\" Curses!\n\n" +
				"FAILURE!",
				requirements.Child.Name, enemyChild.Name);
		}

		return returnObj;
	}

	//QUARTER FINAL MATCH 3
	public static Outcome Event3004(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		// Set enemy family
		Family enemyFamily = null;
		foreach(Family enemy in manager.LeagueFamilies)
		{
			if (enemy.Grandpa.Name == requirements.Grandpa.Name) 
			{
				enemyFamily = enemy;
				break;
			}
		}
		List<Parent> enemyParents = enemyFamily.Parents.OrderBy(o => o.Love).ToList();
		Parent enemyParent = enemyParents[enemyParents.Count - 1];

		if (Constants.Roll(0, requirements.Parent.Love, (int)Enums.Difficulty.STANDARD) && 
			enemyParent.Love < requirements.Parent.Love)
		{
			manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("QUARTERFINAL_MATCH_3_WIN"));
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"{0} daintily kisses every child on the forehead after reading them Good Night Moon. Aww. The judges hearts are full to bursting. {1} gives each child " +
				"a shot of whiskey and some allergy medicine. Not so great.\n\n" +
				"SUCCESS!",
				requirements.Parent.Name, enemyParent.Name);
		}
		else
		{
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format(
				"{1} does an okay job. Screws up tucking the kids into bed. Amateur. However, {0} doesn't even show up. Ouch.\n\n" +
				"FAILURE!",
				requirements.Parent.Name, enemyParent.Name);
		}

		return returnObj;
	}

	//QUARTER FINAL JUDGEMENT
	public static Outcome Event3005(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		int wins = 0;

		if (manager.PlayerFamily.Grandpa.Qualifications.Contains (Qualification.GetQualificationByString ("QUARTERFINAL_MATCH_1_WIN")))
			wins++;
		if (manager.PlayerFamily.Grandpa.Qualifications.Contains (Qualification.GetQualificationByString ("QUARTERFINAL_MATCH_2_WIN")))
			wins++;
		if (manager.PlayerFamily.Grandpa.Qualifications.Contains (Qualification.GetQualificationByString ("QUARTERFINAL_MATCH_3_WIN")))
			wins++;
		
		if (wins > 1)
		{
			manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("QUARTERFINAL_WINNER"));

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"With your {1} wins, you won the quarter finals! {0} and his family slink off into the distance! Next round will be against {2} and his family!\n\n",
				requirements.Grandpa.Name, wins.ToString(), manager.LeagueFamilies[manager.LeagueFamilies.Count - 2].Grandpa.Name);
			returnObj.Mail = new Mail();
			returnObj.Mail.Sender = manager.LeagueFamilies[manager.LeagueFamilies.Count - 2].Grandpa.Name;
			returnObj.Mail.Date = manager.Calendar.GetCurrentDay();
			returnObj.Mail.Subject = "Semifinal Matchup";
			returnObj.Mail.Message = string.Format(
				"{0},\n\n\t" +
				"How are you, kid? I expect a good, clean fight. Me and my family ain't goin' down easy. Best get ready to take some hits.\n\n" +
				"From,\n" +
				"{1}", 
				manager.PlayerFamily.Grandpa.Name, manager.LeagueFamilies[manager.LeagueFamilies.Count - 2].Grandpa.Name);
		}
		else
		{
			manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("TOURNAMENT_LOSER"));

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format(
				"With your {1} wins, you lost the quarter finals! {0} and his family cheer as you all slink off into the distance!\n\n",
				requirements.Grandpa.Name, wins.ToString());
		}

		manager.PlayerFamily.Grandpa.RemoveQualification(Qualification.GetQualificationByString("QUARTERFINAL_MATCH_1_WIN"));
		manager.PlayerFamily.Grandpa.RemoveQualification(Qualification.GetQualificationByString("QUARTERFINAL_MATCH_2_WIN"));
		manager.PlayerFamily.Grandpa.RemoveQualification(Qualification.GetQualificationByString("QUARTERFINAL_MATCH_3_WIN"));

		return returnObj;
	}

	//SEMI FINAL MATCH 1
	public static Outcome Event3006(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (Constants.Roll(0, manager.PlayerFamily.Grandpa.Wisdom, (int)Enums.Difficulty.STANDARD) && 
			requirements.Grandpa.Wisdom < manager.PlayerFamily.Grandpa.Wisdom)
		{
			manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("SEMIFINAL_MATCH_1_WIN"));
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"Grandpa vs {0}. A fight for the ages. Both sit a grandchild on their knee and try to talk them through a difficult situation at school... But " +
				"{0} falters! He doesn't know what text messages are! How can he give relevant advice? Point Grandpa!\n\n" +
				"SUCCESS!",
				requirements.Grandpa.Name);
		}
		else
		{
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format(
				"Grandpa just won't stop shouting about \"kids these days\". The judge can't even get a word in about the rules on the contest! {0} wins by default.\n\n" +
				"FAILURE!",
				requirements.Grandpa.Name);
		}

		return returnObj;
	}

	//SEMI FINAL MATCH 2
	public static Outcome Event3007(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		// Set enemy family
		Family enemyFamily = null;
		foreach(Family enemy in manager.LeagueFamilies)
		{
			if (enemy.Grandpa.Name == requirements.Grandpa.Name) 
			{
				enemyFamily = enemy;
				break;
			}
		}
		List<Child> enemyChildren = enemyFamily.Children.OrderBy(o => o.Intelligence).ToList();
		Child enemyChild = enemyChildren[enemyChildren.Count - 1];

		if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Intelligence, (int)Enums.Difficulty.STANDARD) && 
			enemyChild.Intelligence < requirements.Child.Intelligence)
		{
			manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("SEMIFINAL_MATCH_2_WIN"));
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"The subject matter will be... Bavarian history! {0}'s eyes light up! It's {2} favorite subject of all time! {1} doesn't stand a chance.\n\n" +
				"SUCCESS!",
				requirements.Child.Name, enemyChild.Name, Convert.ToBoolean(requirements.Child.Gender) ? "her" : "his");
		}
		else
		{
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format(
				"This time the quiz subject will be... {1}'s Family History! WHAT? Collusion! But the judge isn't having it.\n\n" +
				"FAILURE!",
				requirements.Child.Name, enemyChild.Name);
		}

		return returnObj;
	}

	//SEMI FINAL MATCH 3
	public static Outcome Event3008(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		// Set enemy family
		Family enemyFamily = null;
		foreach(Family enemy in manager.LeagueFamilies)
		{
			if (enemy.Grandpa.Name == requirements.Grandpa.Name) 
			{
				enemyFamily = enemy;
				break;
			}
		}
		List<Parent> enemyParents = enemyFamily.Parents.OrderBy(o => o.Popularity).ToList();
		Parent enemyParent = enemyParents[enemyParents.Count - 1];

		if (Constants.Roll(0, requirements.Parent.Popularity, (int)Enums.Difficulty.STANDARD) && 
			enemyParent.Popularity < requirements.Parent.Popularity)
		{
			manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("SEMIFINAL_MATCH_3_WIN"));
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"{0}'s burgers and hot dogs are to die for. What is the secret ingredient? {2} refuses to say. {1} burns everything. No one wants any of their food.\n\n" +
				"SUCCESS!",
				requirements.Parent.Name, enemyParent.Name, Convert.ToBoolean(requirements.Parent.Gender) ? "She" : "He");
		}
		else
		{
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format(
				"{0} burns their eyebrows off. Nice going. Default win to {1}!\n\n" +
				"FAILURE!",
				requirements.Parent.Name, enemyParent.Name);
		}

		return returnObj;
	}

	//SEMI FINAL JUDGEMENT
	public static Outcome Event3009(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		int wins = 0;

		if (manager.PlayerFamily.Grandpa.Qualifications.Contains (Qualification.GetQualificationByString ("SEMIFINAL_MATCH_1_WIN")))
			wins++;
		if (manager.PlayerFamily.Grandpa.Qualifications.Contains (Qualification.GetQualificationByString ("SEMIFINAL_MATCH_2_WIN")))
			wins++;
		if (manager.PlayerFamily.Grandpa.Qualifications.Contains (Qualification.GetQualificationByString ("SEMIFINAL_MATCH_3_WIN")))
			wins++;

		if (wins > 1)
		{
			manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("SEMIFINAL_WINNER"));

			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"With your {1} wins, you won the semi finals! {0} and his family slink off into the distance! The final battle isn't going to be easy. " +
				"{2} and his family are going to be the hardest challenge yet...\n\n",
				requirements.Grandpa.Name, wins.ToString(), manager.LeagueFamilies[manager.LeagueFamilies.Count - 1].Grandpa.Name);
			returnObj.Mail = new Mail();
			returnObj.Mail.Sender = manager.LeagueFamilies[manager.LeagueFamilies.Count - 1].Grandpa.Name;
			returnObj.Mail.Date = manager.Calendar.GetCurrentDay();
			returnObj.Mail.Subject = "Grand Final Matchup";
			returnObj.Mail.Message = string.Format(
				"Dearest {0},\n\n\t" +
				"I don't think we've had the pleasure of meeting in person. My name is {1}, and I am the one who will be defeating you and your " +
				"clan next week. It would be in your best interest to not show up. I will not defile my family name losing to the likes of you.\n\n" +
				"Cordially yours,\n" +
				"{1}", 
				manager.PlayerFamily.Grandpa.Name, manager.LeagueFamilies[manager.LeagueFamilies.Count - 1].Grandpa.Name);
		}
		else
		{
			manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("TOURNAMENT_LOSER"));

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format(
				"With your {1} wins, you lost the semi finals! {0} and his family cheer as you all slink off into the distance!\n\n",
				requirements.Grandpa.Name, wins.ToString());
		}

		manager.PlayerFamily.Grandpa.RemoveQualification(Qualification.GetQualificationByString("SEMIFINAL_MATCH_1_WIN"));
		manager.PlayerFamily.Grandpa.RemoveQualification(Qualification.GetQualificationByString("SEMIFINAL_MATCH_2_WIN"));
		manager.PlayerFamily.Grandpa.RemoveQualification(Qualification.GetQualificationByString("SEMIFINAL_MATCH_3_WIN"));

		return returnObj;
	}

	//GRAND FINAL MATCH 1
	public static Outcome Event3010(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		if (!Constants.Roll(0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.STANDARD) && 
			requirements.Grandpa.Insanity > manager.PlayerFamily.Grandpa.Insanity)
		{
			manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("GRANDFINAL_MATCH_1_WIN"));
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"The family has never looked so nervous in their life. But Grandpa does it! Someone. Everyone is baffled, actually. His argument and historical " +
				"sources are actually very compelling. Good job. Maybe Grandpa isn't nuts?\n\n" +
				"SUCCESS!");
		}
		else
		{
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format(
				"Grandpa! No! Stop eating the paper!\n\n" +
				"FAILURE!");
		}

		return returnObj;
	}

	//GRAND FINAL MATCH 2
	public static Outcome Event3011(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		// Set enemy family
		Family enemyFamily = null;
		foreach(Family enemy in manager.LeagueFamilies)
		{
			if (enemy.Grandpa.Name == requirements.Grandpa.Name) 
			{
				enemyFamily = enemy;
				break;
			}
		}
		List<Parent> enemyParents = enemyFamily.Parents.OrderBy(o => o.Intelligence).ToList();
		Parent enemyParent = enemyParents[enemyParents.Count - 1];

		if (Constants.Roll(0, requirements.Parent.Intelligence, (int)Enums.Difficulty.STANDARD) && 
			enemyParent.Intelligence < requirements.Parent.Intelligence)
		{
			manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("GRANDFINAL_MATCH_2_WIN"));
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"{0} has an encyclopedic knowledge of Grandpa! {2}'s blowing everyone away. Even the judges are a little freaked out. {1} is " +
				"sitting in the corner crying.\n\n" +
				"SUCCESS!",
				requirements.Parent.Name, enemyParent.Name, Convert.ToBoolean(requirements.Parent.Gender) ? "She" : "He");
		}
		else
		{
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format(
				"Grandpa who? {0} stares defiantly at Grandpa. {2} is apparently really pissed off about doing dumb quiz shows instead of hanging " +
				"out as a family. Grandpa doesn't care, though.\n\n" +
				"FAILURE!",
				requirements.Parent.Name, enemyParent.Name, Convert.ToBoolean(requirements.Parent.Gender) ? "She" : "He");
		}

		return returnObj;
	}

	//GRAND FINAL MATCH 3
	public static Outcome Event3012(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		// Set enemy family
		Family enemyFamily = null;
		foreach(Family enemy in manager.LeagueFamilies)
		{
			if (enemy.Grandpa.Name == requirements.Grandpa.Name) 
			{
				enemyFamily = enemy;
				break;
			}
		}
		List<Child> enemyChildren = enemyFamily.Children.OrderBy(o => o.Cuteness).ToList();
		Child enemyChild = enemyChildren[enemyChildren.Count - 1];

		if (Constants.Roll(0, requirements.Child.Cuteness, (int)Enums.Difficulty.STANDARD) && 
			enemyChild.Cuteness < requirements.Child.Cuteness)
		{
			manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("GRANDFINAL_MATCH_3_WIN"));
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"{0} is undeniably adorable. Everyone in the audience lets out an audible \"Awwwww...\" whenever {2} does basically anything on stage. " +
				"{1} falls flat on {3} face. Wow, very impressive... NOT!\n\n" +
				"SUCCESS!",
				requirements.Child.Name, enemyChild.Name, Convert.ToBoolean(requirements.Child.Gender) ? "she" : "he", Convert.ToBoolean(enemyChild.Gender) ? "her" : "his");
		}
		else
		{
			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format(
				"{0} has put on a few pounds... Maybe it was that BBQ event from before? Either way, no one is impessed. {1} wins!\n\n" +
				"FAILURE!",
				requirements.Child.Name, enemyChild.Name);
		}

		return returnObj;
	}

	//GRAND FINAL JUDGEMENT
	public static Outcome Event3013(DataManager manager, Requirement requirements)
	{
		Outcome returnObj = new Outcome();

		int wins = 0;

		if (manager.PlayerFamily.Grandpa.Qualifications.Contains (Qualification.GetQualificationByString ("GRANDFINAL_MATCH_1_WIN")))
			wins++;
		if (manager.PlayerFamily.Grandpa.Qualifications.Contains (Qualification.GetQualificationByString ("GRANDFINAL_MATCH_2_WIN")))
			wins++;
		if (manager.PlayerFamily.Grandpa.Qualifications.Contains (Qualification.GetQualificationByString ("GRANDFINAL_MATCH_3_WIN")))
			wins++;

		if (wins > 1)
		{
			manager.PlayerFamily.Grandpa.Pride += Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.SUCCESS;
			returnObj.OutcomeDescription = String.Format(
				"With your {1} wins, you won the grand finals! It's the happiest day of Grandpa's life. Confetti and balloons fall from the " +
				"ceiling and the entire family embraces.\n\n",
				requirements.Grandpa.Name, wins.ToString());
		}
		else
		{
			//manager.PlayerFamily.Grandpa.AddQualification(Qualification.GetQualificationByString("TOURNAMENT_LOSER"));

			manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

			returnObj.Status = (int)Enums.EventOutcome.FAILURE;
			returnObj.OutcomeDescription = String.Format(
				"With your {1} wins, you lost the grand finals! Grandpa slams his fist through a TV screen! No! So close! Why, God, why!!??\n\n",
				requirements.Grandpa.Name, wins.ToString());
		}

		manager.PlayerFamily.Grandpa.RemoveQualification(Qualification.GetQualificationByString("SEMIFINAL_MATCH_1_WIN"));
		manager.PlayerFamily.Grandpa.RemoveQualification(Qualification.GetQualificationByString("SEMIFINAL_MATCH_2_WIN"));
		manager.PlayerFamily.Grandpa.RemoveQualification(Qualification.GetQualificationByString("SEMIFINAL_MATCH_3_WIN"));

		return returnObj;
	}

    //grand final consolation
    public static Outcome Event3014(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();
        ret.OutcomeDescription = string.Format(
			"Grandpa sighs. He felt like he deserved the win last year... Well, there's always next year! If only it wasn't for his shitty family! " +
                                                "Grandpa's negative attitude propagates throughout his entire family\n\nGrandpa's insanity Up!\nFamily slightly depressed!");

        foreach(Character ch in manager.PlayerFamily.GetAllCharacters())
        {
            if(ch.GetType() == typeof(Grandpa))
            {
                ((Grandpa)ch).Insanity += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
                ((Grandpa)ch).Wisdom -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
            }
            else if (ch.GetType() == typeof(Parent))
            {
                ((Parent)ch).Intelligence -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT * 2;
                ((Parent)ch).Popularity -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT * 2;
                ((Parent)ch).Love -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT * 2;
            }
            else if (ch.GetType() == typeof(Child))
            {
                ((Child)ch).Intelligence -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT * 2;
                ((Child)ch).Popularity -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT * 2;
                ((Child)ch).Artistry -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT * 2;
                ((Child)ch).Athleticism -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT * 2;
                ((Child)ch).Cuteness -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT * 2;
            }
        }

        ret.Mail = new Mail();
        ret.Mail.Sender = "Saint Nicholas";
        ret.Mail.Date = manager.Calendar.GetCurrentDay();
        ret.Mail.Subject = "Merry Christmas!";
        ret.Mail.Message = string.Format("{0},\n\nHo ho ho! Merrrry Christmas!! Maybe next year, {0}, maybe next year.\n\nUntil then!\n\"Santa Claus\"", manager.PlayerFamily.Grandpa.Name);

        return ret;
    }

    //HALLOWEEN
    public static Outcome Event3015(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();

        if(!Constants.Roll(0, manager.PlayerFamily.Grandpa.Insanity, (int)Enums.Difficulty.STANDARD) && Constants.Roll(0, manager.PlayerFamily.Grandpa.Wisdom, (int)Enums.Difficulty.STANDARD))
        {
            ret.OutcomeDescription = string.Format(
				"Grandpa momentarily loses sight of {0} when a \"Monster Mash\" flash mob floods the streets. " +
				"While searching for {2}, he comes across {1} and his grandson. Grandpa reaches for his rusty spoon but decides family is more important and " +
				"continues searching for {0}. He finds {2} passed out from over-exposure to chocolate.\n\nGrandpa's pride up.\nGrandpa's wisdom way up!\n{0}'s popularity up.", 
				requirements.Child.Name, requirements.Grandpa.Name, Convert.ToBoolean(requirements.Child.Gender) ? "her" : "him");

            manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
            manager.PlayerFamily.Grandpa.Wisdom += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
            requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
        }
        else
        {
            ret.OutcomeDescription = string.Format(
				"Grandpa momentarily loses sight of {0} when a \"Spooky Scary Skeletons\" flash mob floods the streets. " +
				"While searching for {2} he comes across {1} and his grandson. It isn't long before the chance meeting devolves into a shouting match. " +
				"Grandpa keeps yelling about {1} stealing his grandson for a human sacrifice. Hmm, strange. {0} eventually wanders around the corner and is really embarassed by Grandpa... " +
				"\n\nGrandpa's pride down.\nGrandpa's insanity up.\n{0}'s popularity down.", 
				requirements.Child.Name, requirements.Grandpa.Name, Convert.ToBoolean(requirements.Child.Gender) ? "her" : "him");
            manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
            manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
        }

        return ret;
    }

    //school band bullying
    public static Outcome Event3016(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();

        if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Popularity, (int)Enums.Difficulty.HARD))
        {
            ret.OutcomeDescription = string.Format("While walking home from school one day with their tuba, {0} is confronted by bullies. Rather than fight, {1} puts down their tuba and says: " + 
                                                    "\"Let's go beat up some nerds!\" Together they whack at least half of the student band, ouch!" +
                                                     "\n\n{0}'s popularity up!\n{0}'s artistry slightly down!\nGrandpa's pride up!", requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "she" : "he");

            manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
            requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            requirements.Child.Artistry -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
            ret.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
        }
        else if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.HARD))
        {
			ret.OutcomeDescription = string.Format("While walking home from school one day with their tuba, {0} is confronted by bullies. Instead of running away {1} takes out {2} tuba and begins " +
                                                    "playing the most beautiful song! {1} brings the bullies to tears and their eardrums begin to bleed. Well Tuba'd, {0}!" + 
                                                     "\n\n{0}'s popularity slightly down!\n{0}'s artistry up!\nGrandpa's pride up!", 
				requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "she" : "he", Convert.ToBoolean(requirements.Child.Gender) ? "her" : "his");

            manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
            requirements.Child.Popularity -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
            requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            ret.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
        }
        else
        {
            ret.OutcomeDescription = string.Format(
				"While walking home from school one day with their tuba, {0} is confronted by bullies. \"H-Hey guys what's up,\" {1} asks sheepishly as the bullies " +
				"take out {2} tuba and superglue {2} head inside the bell. \"O-okay I'll just wait right here\"" +
				"\n\n{0}'s popularity down!\nGrandpa's pride down!", requirements.Child.Name, Convert.ToBoolean(requirements.Child.Gender) ? "she" : "he", Convert.ToBoolean(requirements.Child.Gender) ? "her" : "his");

            manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
            requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            ret.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
        }

        return ret;
    }

    //school band solo
    public static Outcome Event3017(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();

        manager.PlayerFamily.Grandpa.Money -= requirements.Money;
        if(requirements.Money >= 2000)
        {
            ret.OutcomeDescription = string.Format("{0} got the solo!! \"Great job, kid\" Grandpa says as he gives a *wink* to the band director. " +
                                                               "Now it might be time to actually start practicing...\n\nGrandpa's pride up!\n{0}'s popularity up!", requirements.Child.Name);

            manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
            requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            requirements.Child.AddQualification(Qualification.GetQualificationByString("BAND_SOLO"));

            ret.Status = (int)Enums.EventOutcome.SUCCESS;
        }
        else if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.VERY_HARD))
        {
            ret.OutcomeDescription = string.Format("{0} walks up on stage and lets loose the solo. It is so perfectly executed it brings the entire band to tears. {0} has " +
                                                    "unequivocally played the greatest solo in the history of mankind. No other student even dares to try and beat them!\n\nGrandpa's pride way up!\n" +
                                                    "{0}'s aristry up!\n{0}'s popularity up!", requirements.Child.Name);

            manager.PlayerFamily.Grandpa.Pride += Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;
            requirements.Child.Popularity += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
            requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            requirements.Child.AddQualification(Qualification.GetQualificationByString("BAND_SOLO"));
            ret.Status = (int)Enums.EventOutcome.SUCCESS;
        }
        else if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.HARD))
        {
            ret.OutcomeDescription = string.Format("{0} plays a decent solo... it's not as good as {1}'s grandchild though, who really blows everyone else out of the water... " + 
                                                    "The band director says {0} can play the solo if {1}'s kid is, like, sick, or something. Maybe next year, I guess.\n\nGrandpa's pride down!\n{0}'s popularity down!",
                                                    requirements.Child.Name, manager.LeagueFamilies[0].Grandpa.Name);

            manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
            requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            ret.Status = (int)Enums.EventOutcome.FAILURE;
        }
        else
        {
            ret.OutcomeDescription = string.Format("{0} walks on stage and lets loose the solo, it's... it's... completely horrible. People in the band are crying -- in pain. " +
                                                    "Yo Yo Ma walks up on stage and personally asks {0} to never play the tuba again. That's a little demoralizing!" +
                                                    "\n\nGrandpa's pride down!\n{0}'s popularity down!\n{0}'s artistry down!", requirements.Child.Name);

            manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
            requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            requirements.Child.Artistry -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            ret.Status = (int)Enums.EventOutcome.FAILURE;
        }

        return ret;
    }

    //school band trip
    public static Outcome Event3018(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();

        manager.PlayerFamily.Grandpa.Money -= requirements.Money;
		if (manager.PlayerFamily.Children.Count > 1)
		{
	        if (!requirements.Accept)
	        {
				ret.OutcomeDescription = string.Format("Ugh, Grandpa, you're so lame! The Nazis are NOT going to invade America while I'm gone. You're so lame; you never let me do anything fun! " +
	                                                    "\n\nGrandpa's pride down!\n{0}'s popularity down!\n{0}'s artistry down!\n{0}'s intelligence up!", requirements.Child.Name);

	            manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
	            requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
	            requirements.Child.Artistry -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
	            requirements.Child.Intelligence -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

	            ret.Status = (int)Enums.EventOutcome.FAILURE;
	        }
	        else
	        {
	            ret.OutcomeDescription = string.Format("Wow thanks Grandpa! You're the best!\n\n{0}'s popularity up!\n{0} has left the family for a week!", requirements.Child.Name);

	            requirements.Child.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

	            SimulationEvent followUp = EventManager.GetEventById(3019);
	            followUp.Requirements.Child = requirements.Child;
	            followUp.Requirements.Money = requirements.Money;
	            manager.PlayerFamily.Children.Remove(requirements.Child);

	            manager.Calendar.ScheduleEventInXDays(followUp, 7);

	            ret.Status = (int)Enums.EventOutcome.SUCCESS;
	        }
		}
		else
			ret.Status = (int)Enums.EventOutcome.PASS;

        return ret;
    }
		
	// Band return trip
    public static Outcome Event3019(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();

        if (requirements.Money >= 250 && Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.STANDARD))
        {
            ret.OutcomeDescription = string.Format("The trip was awesome Grandpa! Me and some of the other kids played \"Rock Around the Clock\" at 3am over Elvis' grave to summon him! " +
                                                    "He kept saying something about needing money and he smelled like you do on Saturday nights so I just gave him all the money you gave me!" +
                                                    "\n\nGrandpa's pride way up!\n{0}'s artistry up!\n{0} has rejoined the family!", requirements.Child.Name);


            manager.PlayerFamily.Grandpa.Pride += Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;
            requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

            ret.Status = (int)Enums.EventOutcome.SUCCESS;
        }
        else if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.HARD))
        {
            ret.OutcomeDescription = string.Format("The trip was pretty good, Grandpa (even if I wish I had a little more money...)! We played to a crowd of nearly 4000 diehard Elvis fans " +
                                                    "and impersonators! Did I miss anything good here at home while I was away?\n\nGrandpa's pride up!\n{0}'s artistry up!\n{0} has rejoined the family!", requirements.Child.Name);

            manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
            requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

            ret.Status = (int)Enums.EventOutcome.SUCCESS;
        }
        else
        {
            ret.OutcomeDescription = string.Format("Oh, Grandpa, the trip was horrible! Everyone made fun of me the entire time. They dug up Elvis' grave and buried my tuba with him! " +
                                                    "Why did you let me go!?\n\nGrandpa's pride down!\n{0}'s artistry down!\n{0}'s popularity down\n{0} has rejoined the family!", requirements.Child.Name);

            manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
            requirements.Child.Artistry -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            
			ret.Status = (int)Enums.EventOutcome.FAILURE;
        }

		manager.PlayerFamily.Children.Add(requirements.Child);
		requirements.Child = null;
        return ret;
    }

    //school band skill checkup
    public static Outcome Event3020(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();

        if (!Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.EASY))
        {
            ret.OutcomeDescription = string.Format("\"I'm gonna level with you, Mr. {0}. {1} is the worst goddamn tuba player I have ever heard in my entire life. As much " +
                                                    "as it kills me to do this, I'm gonna have to kick him out of the band. I have never done this in all of my 40 years of instruction " +
                                                    "but {1} is sincerely horrible. So much so that this might somehow be a compliment!\""+
                                                    "\n\n{1} kicked out of band!\nGrandpa's pride way down!\nGrandpa's insanity up!\n{1}'s artistry way down!\n{1}'s popularity down!", 
													manager.PlayerFamily.FamilyName, requirements.Child.Name);

			manager.PlayerFamily.Grandpa.Insanity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;
            requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            requirements.Child.Artistry -= Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;
            requirements.Child.RemoveQualification(Qualification.GetQualificationByString("IN_SCHOOL_BAND"));

            ret.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;
        }
        else
        {
            ret.OutcomeDescription = string.Format("\"{0} is doing very well in band Mr. {1}. If they keep it up they might be one of the greats! Like Bach, Mozart, or ABBA.\"\n\n" +
                                                    "Grandpa's pride up!\n{0}'s artistry up!", requirements.Child.Name, manager.PlayerFamily.FamilyName);

            manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
            requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;

            ret.Status = (int)Enums.EventOutcome.SUCCESS_BLACKLIST_YEAR;
        }

        return ret;
    }

    //school band final concert
    public static Outcome Event3021(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();

        if(requirements.Child.Qualifications.Contains(Qualification.GetQualificationByString("BAND_SOLO")))
        {
            if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.VERY_HARD))
            {
                ret.OutcomeDescription = string.Format("The concert has gone absolutely perfect so far, then {0} stands up to perform their solo. All eyes are on them as they begin to " +
                                                        "play the sweet sweet sounds of Mozart. {1} is immediately brought to tears and looks around and sees that everyone else in the audience is crying. " +
                                                        "Grandpa looks over and catches {2} crying too, who immediately wipes away his tears and puts on a scowl. Success!\n\n" +
                                                        "Grandpa's pride way up!!\n{1}'s popularity up!\n{1}'s love up!\n{0}'s artistry way up!", requirements.Child.Name, requirements.Parent.Name, manager.LeagueFamilies[0].Grandpa.Name);

                requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
                requirements.Parent.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
                requirements.Parent.Love += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
                manager.PlayerFamily.Grandpa.Pride += Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;

                ret.Mail = new Mail();
                ret.Mail.StringDate = "December 9, 1780";
                ret.Mail.Sender = "Wolfgang Amadeus Mozart";
                ret.Mail.Subject = "RE: Your grandson";
                ret.Mail.Message = string.Format("Hello {0},\nIt's me, Mozart. I'm writing to you just to let you know that your grandchild is the single greatest musician in all of human history.\nKeep it real,\nWolfgang Amadeus Mozart", manager.PlayerFamily.Grandpa.Name);

                ret.Status = (int)Enums.EventOutcome.SUCCESS;
            }
            else if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.STANDARD))
            {
                ret.OutcomeDescription = string.Format("The concert is going pretty well, then {0} stands up to perform their solo. All eyes are on them as they begin to " +
                                                        "play those beautiful notes. Er, pretty beautiful. They mess up a little bit but recover nicely. By all means a successful concert! " +
                                                        "Grandpa's pride up!!\n{1}'s love up!\n{0}'s artistry up!", requirements.Child.Name, requirements.Parent.Name);

                requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
                requirements.Parent.Love += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
                manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

                ret.Status = (int)Enums.EventOutcome.SUCCESS;
            }
            else
            {
                ret.OutcomeDescription = string.Format("The concert has been a little rocky, but at least {0} has been doing good...you think? {0} stands up to perform their solo. All eyes are on them as they begin to " +
                                                        "play the bars. They botch it up pretty bad. Ha, ha, can't get much worse than this! But it does. {1} is in the audience and throws a piece of rotten fruit. Next thing you " +
                                                        "know {0} being pummeled with spoiled vegetation. They burst into tears and run off the stage. Grandpa quietly slides out of the concert hall.\n\n" +
                                                        "Grandpa's pride way down!!\n{1}'s love down!\n{0}'s artistry down!\n{0}'s popularity down!", requirements.Child.Name, manager.LeagueFamilies[0].Grandpa.Name);

                requirements.Child.Artistry -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
                requirements.Child.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
                requirements.Parent.Love -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
                manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;


                ret.Mail = new Mail();
                ret.Mail.Date = manager.Calendar.GetCurrentDay();
                ret.Mail.Sender = manager.LeagueFamilies[0].Grandpa.Name;
                ret.Mail.Subject = "Enjoy the fruit salad?";
                ret.Mail.Message = string.Format("Hey loser,\nEnjoy that fruit salad the other day? You better keep your loser grandchild away from my grandkids, wouldn't want them to become talentless!\nDeuces,\n{0}", manager.LeagueFamilies[0].Grandpa.Name);

                ret.Status = (int)Enums.EventOutcome.FAILURE;
            }
        }
        else
        {
            if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.STANDARD))
            {
                ret.OutcomeDescription = string.Format("The concert is going pretty well, then {0}'s grandson stands up to perform their solo. All eyes are on them as they begin to " +
                                                        "play those beautiful notes. It is amazing. You would never admit it but you wish you had a talented grandchild like that... maybe {0} " +
                                                        "would be willing to strike up a deal... " +
                                                        "Grandpa's pride up!\n{1}'s artistry up!", manager.LeagueFamilies[0].Grandpa.Name, requirements.Child.Name);

                requirements.Child.Artistry += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
                manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

                ret.Status = (int)Enums.EventOutcome.SUCCESS;
            }
            else
            {
                ret.OutcomeDescription = string.Format("The concert has been a little rocky, but at least {0} has been doing good...you think? {0} moves to adjust their music stand and falls off the stage! All eyes are on them as they begin to " +
                                                        "sob uncontrollably on the floor of the concert hall. Ha, ha, can't get much worse than this! But it does. {1} is in the audience and throws a piece of rotten fruit. Next thing you " +
                                                        "know {0} being pummeled with spoiled vegetation. {0} runs out of the conert hall but not before tripping and breaking their tuba clean in half. Grandpa quietly slides out of the concert.\n\n" +
                                                        "Grandpa's pride way down!!\n{2}'s love down!\n{0}'s artistry down!\n{2}'s popularity down!", requirements.Child.Name, manager.LeagueFamilies[0].Grandpa.Name, requirements.Parent.Name);

                requirements.Child.Artistry -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
                requirements.Parent.Love -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
                requirements.Parent.Popularity -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
                manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MAJOR_PRIDE_CHANGE_AMOUNT;


                ret.Mail = new Mail();
                ret.Mail.Date = manager.Calendar.GetCurrentDay();
                ret.Mail.Sender = manager.LeagueFamilies[0].Grandpa.Name;
                ret.Mail.Subject = "Enjoy the fruit salad?";
                ret.Mail.Message = string.Format("Hey loser,\nEnjoy that fruit salad the other day? You better keep your loser grandchild away from my grandkids, wouldn't want them to become talentless!\nDeuces,\n{0}", manager.LeagueFamilies[0].Grandpa.Name);

                ret.Status = (int)Enums.EventOutcome.FAILURE;
            }
        }

        return ret;
    }

    //x goes into a coma
    public static Outcome Event3022(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();

        if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Athleticism, (int)Enums.Difficulty.HARD))
        {
            ret.Status = (int)Enums.EventOutcome.PASS;
        }
		else if (manager.PlayerFamily.Children.Count > 1)
        {
            ret.OutcomeDescription = string.Format("{0} is playing by the Leagueville cliffs with {1} when they fall off the side!! Grandpa is suspicious of {1} but " +
                                                    "{0} is quickly rushed into the hospital. Bad news. {0} is in a deep coma and it doesn't look like he'll be coming out any time soon.\n\n" +
                                                    "Grandpa's pride down!\n{0} is now in a coma!", requirements.Child.Name, manager.LeagueFamilies[0].Children[0].Name + " " + manager.LeagueFamilies[0].FamilyName);

            manager.PlayerFamily.Grandpa.Pride -= Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;
            SimulationEvent followUp = EventManager.GetEventById(3023);
            followUp.DoNotDestroy = true;
            followUp.Requirements.Child = requirements.Child;
            manager.PlayerFamily.Children.Remove(requirements.Child);
            manager.Calendar.ScheduleEventInXDays(followUp, 7);

            ret.Status = (int)Enums.EventOutcome.FAILURE_BLACKLIST_YEAR;

            ret.Mail = new Mail();
            ret.Mail.Date = manager.Calendar.GetCurrentDay();
            ret.Mail.Sender = "Doctor Fluffernutter";
            ret.Mail.Subject = "Your grandson";
            ret.Mail.Message = string.Format("Hello,\n\n{0} is still in a deep deep sleep. I don't think he will die, but I don't know how long it will be until he wakes up. You should visit every once in a while, maybe it will trigger something in him.\n\nYours,\nDoctor Fluffernutter", requirements.Child.Name);

            requirements.Child = null;
        }
		else
			ret.Status = (int)Enums.EventOutcome.PASS;

        return ret;
    }

    //hospital visit
    public static Outcome Event3023(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();

		if (Constants.Roll(0, requirements.Parent.Love, (int)Enums.Difficulty.HARD) || (manager.Calendar.Day >= 26 && manager.Calendar.Month == 11) || (Constants.RANDOM.Next(1,100) <= 10))
        {
            ret.OutcomeDescription = string.Format("{0} tries their hardest to wake {1} up. By some miracle he begins to rouse from his deep slumber! {1} woke up! The " +
                                                    "power of love has set {1} free!!\n\n{0}'s love way up!\n{1} has rejoined the family!", requirements.Parent.Name, requirements.Child.Name);

            requirements.Parent.Love += Constants.Character.MAJOR_STAT_CHANGE_AMOUNT;

            SimulationEvent followUp = EventManager.GetEventById(3023);
            followUp.DoNotDestroy = false;
            manager.PlayerFamily.Children.Add(requirements.Child);
            requirements.Child = null;

            ret.Status = (int)Enums.EventOutcome.SUCCESS;
        }
        else
        {
            ret.OutcomeDescription = string.Format("{0} tries their hardest to wake {1} up, but its no use. He's off in la-la dream land. He mumbles something about " +
                                                    "unicorns and World War II before falling back into a deep slumber.\n\n{0}'s love up slightly!\n{1}'s stats down slightly!", requirements.Parent.Name, requirements.Child.Name);

            requirements.Parent.Love += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

            requirements.Child.Intelligence -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
            requirements.Child.Artistry -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
            requirements.Child.Athleticism -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
            requirements.Child.Cuteness -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
            requirements.Child.Popularity -= Constants.Character.MINOR_STAT_CHANGE_AMOUNT;

            ret.Status = (int)Enums.EventOutcome.FAILURE;
            manager.Calendar.ScheduleEventInXDays(EventManager.GetEventById(3023), 7);
        }

        return ret;
    }

    //parent teacher conference
    public static Outcome Event3024(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();

        if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Intelligence, (int)Enums.Difficulty.HARD) && Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.EASY))
        {
            ret.OutcomeDescription = string.Format("{0}'s teacher says that they're the smartest kid in the entire class! Wowza that is some good parenting, {1}. In fact, the teacher nominates you for parent of the year, what an honor! " +
                                                    "\n\nGrandpa's pride up!\n{1}'s love up!\n{1}'s popularity up!", requirements.Child.Name, requirements.Parent.Name);

            requirements.Parent.Love += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            requirements.Parent.Popularity += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            manager.PlayerFamily.Grandpa.Pride += Constants.Character.STANDARD_PRIDE_CHANGE_AMOUNT;

            ret.Status = (int)Enums.EventOutcome.SUCCESS;
        }
        else if (Constants.Roll(requirements.Child.Cuteness, requirements.Child.Intelligence, (int)Enums.Difficulty.STANDARD) && Constants.Roll(requirements.Child.Cuteness, requirements.Child.Artistry, (int)Enums.Difficulty.VERY_EASY))
        {
            ret.OutcomeDescription = string.Format("{0}'s teacher says that {0} is pretty average intelligence. Whew, dodged a bullet there. Grandpa was worried they'd end up stupid like Grandma was." +
                                                    "\n\nGrandpa's pride up!\n{1}'s love up slightly!", requirements.Child.Name, requirements.Parent.Name);

            requirements.Parent.Love += Constants.Character.MINOR_STAT_CHANGE_AMOUNT;
            manager.PlayerFamily.Grandpa.Pride += Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

            ret.Status = (int)Enums.EventOutcome.SUCCESS;
        }
        else
        {
            ret.OutcomeDescription = string.Format("{0}'s teacher says that {0} is dumb as dirt. Literally. When they were outside for a science project {0} couldn't tell the difference between dirt and an American Elm tree. " +
                                                    "\n\nGrandpa's pride down!\n{1}'s love down!", requirements.Child.Name, requirements.Parent.Name);

            requirements.Parent.Love -= Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            manager.PlayerFamily.Grandpa.Pride -= Constants.Character.MINOR_PRIDE_CHANGE_AMOUNT;

            SimulationEvent followUp = EventManager.GetEventById(3025);
            followUp.Requirements.Child = requirements.Child;
            manager.Calendar.ScheduleEventInXDays(followUp, 4);

            ret.Status = (int)Enums.EventOutcome.FAILURE;
        }

        return ret;
    }

    //professional tutor
    public static Outcome Event3025(DataManager manager, Requirement requirements)
    {
        Outcome ret = new Outcome();

        if(!requirements.Accept)
        {
            ret.Status = (int)Enums.EventOutcome.PASS;
            return ret;
        }

        if ((manager.PlayerFamily.Grandpa.MoneyGrowth - manager.PlayerFamily.Upkeep) < 200)
        {
            ret.OutcomeDescription = string.Format("Hey man, are you tring to rip me off? You can't afford that!");
            ret.Status = (int)Enums.EventOutcome.FAILURE;
            return ret;
        }
        else
        {
            manager.PlayerFamily.Grandpa.MoneyGrowth -= 200;
            ret.OutcomeDescription = string.Format("Thanks, we'll get {0} back on track to being the next Hans Gruber in no time!\n\n{0}'s intelligence up!\n{0}'s intelligence growth up!", requirements.Child.Name);

            requirements.Child.Intelligence += Constants.Character.STANDARD_STAT_CHANGE_AMOUNT;
            requirements.Child.IntelligenceGrowth += Constants.Character.STANDARD_STAT_GROWTH_AMOUNT;

            ret.Status = (int)Enums.EventOutcome.SUCCESS;
        }

        return ret;
    }

}
