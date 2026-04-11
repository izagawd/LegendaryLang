struct Foo {
    x: i32
}

fn main() -> i32 {
    let b: Gc(Foo) = Gc.New(make Foo { x: 42 });
    let f: Foo = *b;
    f.x
}
