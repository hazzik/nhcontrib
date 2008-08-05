using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NHibernate.Criterion;
using NHibernate.Dialect.Function;
using NHibernate.Linq.Expressions;
using NHibernate.Linq.Util;

namespace NHibernate.Linq.Visitors.MethodTranslators
{
	public class StringMethodTranslator:IMethodTranslator
	{
		#region IMethodTranslator Members
		public StringMethodTranslator()
		{
		}

		public void Initialize(ISession session, ICriteria rootCriteria)
		{

			this.criteria = rootCriteria;
			this.session = session;
		}

		private ICriteria criteria;
		private ISession session;




		public ProjectionWithImplication GetProjection(MethodCallExpression expression)
		{
			switch (expression.Method.Name)
			{
				case "StartsWith":
					return GetLikeCriterion(expression, MatchMode.Start);
				case "EndsWith":
					return GetLikeCriterion(expression, MatchMode.End);
				case "Equals":
					return GetLikeCriterion(expression, MatchMode.Exact);
				case "Contains":
					return GetLikeCriterion(expression, MatchMode.Anywhere);
				case "Replace":
					return GetReplaceProjection(expression);
				case "ToUpper":
					return GetToUpperProjection(expression);
				case "IndexOf":
					return GetIndexOfProjection(expression);
				case "Substring":
					return GetSubstringProjection(expression);
				default:
					throw new NotImplementedException(expression.Method.Name);
					break;
			}
		}



		#endregion

		protected virtual ProjectionWithImplication GetSubstringProjection(MethodCallExpression expr)
		{
			var function = new StandardSQLFunction("substring");

			var source = expr.Object;

			var projections = new IProjection[expr.Arguments.Count + 1];

			var objVisitor = new SelectArgumentsVisitor(this.criteria, this.session);
			objVisitor.Visit(source);
			projections[0] = objVisitor.Projection;

			var p0 = expr.Arguments[0];
			var p0Visitor = new SelectArgumentsVisitor(this.criteria, this.session);
			p0Visitor.Visit(p0);
			projections[1] = Projections.SqlFunction(SelectArgumentsVisitor.arithmaticAddition,
			                                         NHibernateUtil.GuessType(p0.Type), p0Visitor.Projection,
			                                         Projections.Constant(1));
			if (expr.Arguments.Count > 1)
			{
				var p1 = expr.Arguments[1];
				var p1Visitor = new SelectArgumentsVisitor(this.criteria, this.session);
				p1Visitor.Visit(p1);
				projections[2] = p1Visitor.Projection;
			}
			else
			{
				projections[2] = Projections.Constant(Int32.MaxValue);
			}

			return new ProjectionWithImplication(Projections.SqlFunction(function, NHibernateUtil.Int32, projections));
		}

		protected virtual ProjectionWithImplication GetIndexOfProjection(MethodCallExpression expr)
		{
			var function = new StandardSQLFunction("charindex");
			var obj = expr.Object;

			var projections = new IProjection[expr.Arguments.Count + 1];

			var objVisitor = new SelectArgumentsVisitor(this.criteria, this.session);
			objVisitor.Visit(obj);
			projections[0] = objVisitor.Projection;

			var p0 = expr.Arguments[0];
			var p0Visitor = new SelectArgumentsVisitor(this.criteria, this.session);
			p0Visitor.Visit(p0);
			projections[1] = p0Visitor.Projection;
			if(expr.Arguments.Count>1)
			{
				var p1 = expr.Arguments[1];
				var p1Visitor = new SelectArgumentsVisitor(this.criteria, this.session);
				p1Visitor.Visit(p1);
				projections[2] = Projections.SqlFunction(SelectArgumentsVisitor.arithmaticAddition,
				                                         NHibernateUtil.GuessType(p1.Type),
				                                         p1Visitor.Projection,
				                                         Projections.Constant(1));
			}

			return new ProjectionWithImplication(Projections.SqlFunction(function, NHibernateUtil.Int32,projections));
		}
		protected virtual ProjectionWithImplication GetToUpperProjection(MethodCallExpression expr)
		{
			var translator = new DBFunctionMethodTranslator();
			translator.Initialize(this.session, this.criteria);
			return translator.GetProjection("upper",expr.Type,expr.Object);
		}
		protected virtual ProjectionWithImplication GetReplaceProjection(MethodCallExpression expr)
		{
			var function = new StandardSQLFunction("replace");
			var source = expr.Object;
			var p0 = expr.Arguments[0];
			var p1 = expr.Arguments[1];

			var translator = new DBFunctionMethodTranslator();
			translator.Initialize(this.session, this.criteria);
			return translator.GetProjection("replace", expr.Type, source, p0, p1);
		}
		protected virtual ProjectionWithImplication GetLikeCriterion(MethodCallExpression expr, MatchMode matchMode)
		{
			var criterion= Restrictions.Like(MemberNameVisitor.GetMemberName(this.criteria, expr.Object),
			                                 String.Format("{0}", QueryUtil.GetExpressionValue(expr.Arguments[0])),
			                                 matchMode);
			return new ProjectionWithImplication(Projections.Conditional(criterion,
			                               Projections.Constant(true), 
			                               Projections.Constant(false)));
		}
	}
}