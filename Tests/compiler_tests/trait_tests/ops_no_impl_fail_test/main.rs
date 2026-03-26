struct Foo {
    val: i32
}

fn main() -> i32 {
    let a = Foo { val = 1 };
    let b = Foo { val = 2 };
    a + b
}
