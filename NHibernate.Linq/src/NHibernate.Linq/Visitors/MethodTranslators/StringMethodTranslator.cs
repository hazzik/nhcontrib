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




		public IProjection GetProjection(MethodCallExpression expression)
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
				default:
					throw new NotImplementedException(expression.Method.Name);
					break;
			}
		}



		#endregion
		protected virtual IProjection GetIndexOfProjection(MethodCallExpression expr)
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
				projections[2] = p1Visitor.Projection;
			}
			return Projections.SqlFunction(function, NHibernateUtil.Int32,projections);
		}
		protected virtual IProjection GetToUpperProjection(MethodCallExpression expr)
		{
			var obj = expr.Object;
			var visitor = new SelectArgumentsVisitor(this.criteria, this.session);
			visitor.Visit(obj);
			var function = new StandardSQLFunction("upper");
			return Projections.SqlFunction(function,NHibernateUtil.String, visitor.Projection);

		}
		protected virtual IProjection GetReplaceProjection(MethodCallExpression expr)
		{
			var function = new StandardSQLFunction("replace");
			var obj = expr.Object;
			var p0 = expr.Arguments[0];
			var p1 = expr.Arguments[1];
			var objVisitor = new SelectArgumentsVisitor(this.criteria, this.session);
			var p0Visitor = new SelectArgumentsVisitor(this.criteria, this.session);
			var p1Visitor = new SelectArgumentsVisitor(this.criteria, this.session);
			p0Visitor.Visit(p0);
			p1Visitor.Visit(p1);
			objVisitor.Visit(obj);

			return Projections.SqlFunction(function,NHibernateUtil.String,
			                               objVisitor.Projection,
			                               p0Visitor.Projection,
			                               p1Visitor.Projection
				);
		}
		protected virtual IProjection GetLikeCriterion(MethodCallExpression expr, MatchMode matchMode)
		{
			var criterion= Restrictions.Like(MemberNameVisitor.GetMemberName(this.criteria, expr.Object),
			                                 String.Format("{0}", QueryUtil.GetExpressionValue(expr.Arguments[0])),
			                                 matchMode);
			return Projections.Conditional(criterion,
			                               Projections.Constant(true), 
			                               Projections.Constant(false));
		}
	}
}