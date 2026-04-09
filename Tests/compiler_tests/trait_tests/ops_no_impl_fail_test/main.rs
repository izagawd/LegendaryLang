struct Foo {
    val: i32
}

fn main() -> i32 {
    let a = make Foo { val : 1 };
    let b = make Foo { val : 2 };
    a + b
}
