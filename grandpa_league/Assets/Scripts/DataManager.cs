using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DataManager
{
    private PlayerInfo              m_currentInfo       = null;
    private Calendar                m_currentCalendar   = null;

    private List<Family>            m_league            = new List<Family>();
    private Family                  m_playerFamily      = null;
    private Family                  m_orphanage         = new Family();

    private List<int>               m_blacklistYear     = new List<int>();
    private List<int>               m_blacklistForever  = new List<int>();
    private List<Ability>           m_abilities         = new List<Ability>();

    public DataManager (string playerName)
    {
        this.m_currentInfo = new PlayerInfo();
        this.m_currentCalendar = new Calendar();

        this.m_playerFamily = new Family();
        this.m_playerFamily.GrandpaList.Add(new Grandpa(playerName));
        this.m_playerFamily.Grandpa.Money = Constants.Player.INITIAL_MONEY;
        this.m_playerFamily.Grandpa.MoneyGrowth = Constants.Player.INITIAL_INCOME;
        this.m_playerFamily.Grandpa.Wisdom = Constants.Player.INITIAL_WISDOM;
        this.m_playerFamily.Grandpa.WisdomGrowth = Constants.Player.INITIAL_WISDOM_GROWTH;
        this.m_playerFamily.Grandpa.Insanity = Constants.Player.INITIAL_INSANITY;
        this.m_playerFamily.Grandpa.InsanityGrowth = Constants.Player.INITIAL_INSANITY_GROWTH;
		this.m_playerFamily.Grandpa.SpriteName = Constants.Player.SPRITE_NAME;

        //this.m_playerFamily.Parents = CharacterManager.GetRandomParents(Constants.INITIAL_PARENTS);
        //this.m_playerFamily.Children = CharacterManager.GetRandomChildren(Constants.INITIAL_CHILDREN);

        //currently we are giving the player some reliable characters to work with in the start
        this.m_playerFamily.Parents.Add(CharacterManager.GetParentByName("Beth"));
        this.m_playerFamily.Parents.Add(CharacterManager.GetParentByName("Mike"));
        this.m_playerFamily.Children.Add(CharacterManager.GetChildByName("Christopher"));
        this.m_playerFamily.Children.Add(CharacterManager.GetChildByName("Kevin"));
        this.m_playerFamily.Children.Add(CharacterManager.GetChildByName("Patrick"));

		this.m_abilities.Add (EventManager.GetAbilityById (60)); // Stat doubling
		this.m_abilities.Add (EventManager.GetAbilityById (62)); // Event replayer
		this.m_abilities.Add (EventManager.GetAbilityById (63)); // Child sacrifice

        string[] splitName = playerName.Split(' ');
        if (splitName.Length >= 2)
        {
            this.m_playerFamily.FamilyName = splitName[splitName.Length - 1];
            this.m_playerFamily.Grandpa.Name = splitName[0];
        }
        else
        {
            this.m_playerFamily.FamilyName = Constants.Player.DEFAULT_SURNAME;
            this.m_playerFamily.Grandpa.Name = Constants.Player.DEFAULT_FIRST_NAME;
        }

        for (int i = 1; i < Constants.NUM_FAMILIES; i++)
            this.m_league.Add(new Family(true));

        foreach(Child child in CharacterManager.GetRemainingChildren())
        {
            this.m_orphanage.Children.Add(child);
        }

        foreach (Parent parent in CharacterManager.GetRemainingParents())
        {
            this.m_orphanage.Parents.Add(parent);
        }

        foreach (Grandpa grandpa in CharacterManager.GetRemainingGrandpas())
        {
            this.m_orphanage.GrandpaList.Add(grandpa);
        }

        this.m_playerFamily.Mailbox.Add(this.GetInitialMail());
    }

    private Mail GetInitialMail()
    {
        Mail mail = new Mail();
        mail.StringDate = "January 1, 2016";
        mail.Sender = string.Format("{0} {1}", this.m_playerFamily.Parents[0].Name, this.m_playerFamily.FamilyName);
        mail.Subject = "Your new home";
        mail.Message = string.Format(
			"Hey Dad,\n\n\tEnjoying the new digs at our home yet? I promise this is just a temporary " +
			"situation until we can figure out why you were wandering around your neighbor's garage in the middle of the night " +
			"looking for Sniffles. Sniffles has been dead for 10 years, Dad. Anyway, the kids are excited to hang out with you this weekend! " +
			"I'm looking forward to hanging out, too!\n\nLove,\n{0}", this.m_playerFamily.Parents[0].Name);
        return mail;
    }
    
    public Family PlayerFamily
    {
        get { return this.m_playerFamily; }
    }

    public List<Family> LeagueFamilies
    {
        get { return this.m_league; }
		set { this.m_league = value; }
    }

	public List<Ability> Abilities
	{
		get { return this.m_abilities; }
	}

    public Calendar Calendar
    {
        get { return this.m_currentCalendar; }
    }

    public PlayerInfo PlayerInfo
    {
        get { return this.m_currentInfo; }
        set { this.m_currentInfo = value; }
    }

    public List<int> BlacklistYear
    {
        get { return this.m_blacklistYear; }
        set { this.m_blacklistYear = value; }
    }

    public List<int> BlacklistForever
    {
        get { return this.m_blacklistForever; }
        set { this.m_blacklistForever = value; }
    }

    public Family Orphanage
    {
        get { return this.m_orphanage; }
        set { this.m_orphanage = value; }
    }
}