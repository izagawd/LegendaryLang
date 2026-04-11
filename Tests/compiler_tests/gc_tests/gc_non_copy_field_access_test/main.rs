struct Foo {
    x: i32,
    y: i32
}

fn main() -> i32 {
    let b: GcMut(Foo) = GcMut.New(make Foo { x: 10, y: 32 });
    *b = make Foo { x: 5, y: 37 };
    (*b).x + (*b).y
}
