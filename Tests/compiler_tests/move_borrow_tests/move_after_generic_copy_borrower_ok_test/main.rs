struct Foo { val: i32 }
fn TakeOwnership[T:! type](input: T) {}
fn use_copy[T:! Copy](x: T) -> i32 { 0 }
fn main() -> i32 {
    let a = make Foo { val: 5 };
    let r = &a;
    use_copy(r);
    TakeOwnership(a);
    5
}
