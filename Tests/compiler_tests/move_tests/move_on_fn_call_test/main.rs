struct Foo {
    val: i32
}

fn consume(f: Foo) -> i32 {
    f.val
}

fn main() -> i32 {
    let a = make Foo { val : 5 };
    let x = consume(a);
    a.val
}
