﻿using Fixie.Conventions;

namespace Fixie.Behaviors
{
    public class ExecuteCases : InstanceBehavior
    {
        public void Execute(Fixture fixture, Convention convention)
        {
            foreach (var @case in fixture.Cases)
                fixture.CaseExecutionBehavior.Execute(@case.Method, fixture.Instance, @case.Exceptions);
        }
    }
}