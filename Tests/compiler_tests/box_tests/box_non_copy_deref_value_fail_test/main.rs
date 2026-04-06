struct Foo {
    x: i32
}

fn main() -> i32 {
    let b: Box(Foo) = Box.New(make Foo { x: 42 });
    let f: Foo = *b;
    f.x
}
