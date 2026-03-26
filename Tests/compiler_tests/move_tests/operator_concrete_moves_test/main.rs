struct Foo {
    val: i32
}

impl Add<Foo> for Foo {
    type Output = Foo;
    fn add(lhs: Foo, rhs: Foo) -> Foo {
        Foo { val = lhs.val + rhs.val }
    }
}

fn main() -> i32 {
    let a = Foo { val = 1 };
    let b = Foo { val = 2 };
    let c = a + b;
    let d = a + b;
    c.val
}
