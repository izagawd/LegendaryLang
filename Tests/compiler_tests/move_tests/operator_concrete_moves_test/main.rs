use Std.Core.Ops.Add;
struct Foo {
    val: i32
}

impl Add(Foo) for Foo {
    type Output = Foo;
    fn Add(lhs: Foo, rhs: Foo) -> Foo {
        make Foo { val : lhs.val + rhs.val }
    }
}

fn main() -> i32 {
    let a = make Foo { val : 1 };
    let b = make Foo { val : 2 };
    let c = a + b;
    let d = a + b;
    c.val
}
