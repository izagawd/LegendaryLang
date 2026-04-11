struct Foo {
    x: i32
}

fn main() -> i32 {
    let b: GcMut(Foo) = GcMut.New(make Foo { x: 42 });
    let f: Foo = *b;
    f.x
}
