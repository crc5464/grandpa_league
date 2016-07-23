﻿using System;

public class Grandpa : Character
{
    private int     m_insanity          = 0;
    private int     m_wisdom            = 0;
    private int     m_money             = 0;
    private int     m_pride             = 0;

    private double  m_insanityGrowth    = 0;
    private double  m_wisdomGrowth      = 0;
	private double  m_moneyGrowth       = 0;
    
	public Grandpa(string name, int age, int wisdom, int insanity, int money)
	{
        this.m_name = name;
        this.m_age = age;
        this.m_wisdom = wisdom;
        this.m_insanity = insanity;
		this.m_money = money;

        this.m_insanityGrowth = -1 * (this.m_insanity / 300);
        this.m_wisdomGrowth = (this.m_wisdom / 300);
		this.m_moneyGrowth = (this.m_money / 10);
	}

    public Grandpa(string name)
    {
        this.m_name = name;
    }

    public int Wisdom
    {
        get { return this.m_wisdom; }
        set { this.m_wisdom = value; }
    }

    public double WisdomGrowth
    {
        get { return this.m_wisdomGrowth; }
        set { this.m_wisdomGrowth = value; }
    }

    public int Insanity
    {
        get { return this.m_insanity; }
        set { this.m_insanity = value; }
    }

    public double InsanityGrowth
    {
        get { return this.m_insanityGrowth; }
        set { this.m_insanityGrowth = value; }
    }

    public int Money
    {
        get { return this.m_money; }
        set { this.m_money = value; }
    }

	public double MoneyGrowth
	{
		get { return this.m_moneyGrowth; }
		set { this.m_moneyGrowth = value; }
	}

    public int Pride
    {
        get { return this.m_pride; }
        set { this.m_pride = value; }
    }
}