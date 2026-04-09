struct Bar {
    number: i32
}
struct Foo {
    dd: Bar
}
fn main() -> i32 {
    let foo = make Foo { dd: make Bar { number: 4 } };
    let doo = &uniq foo;
    let gotten = &uniq doo.dd.number;
    *gotten = *gotten + 5;
    let p = doo;
    foo.dd.number + p.dd.number
}
