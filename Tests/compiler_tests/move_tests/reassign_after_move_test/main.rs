struct Foo {
    val: i32
}

fn main() -> i32 {
    let a = Foo { val = 5 };
    let b = a;
    a = Foo { val = 10 };
    a.val + b.val
}
