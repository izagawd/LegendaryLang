struct Foo['a] { foo: &'a i32 }
fn main() -> i32 {
    let x: i32 = 42;
    let f = make Foo { foo: &x };
    *f.foo
}
