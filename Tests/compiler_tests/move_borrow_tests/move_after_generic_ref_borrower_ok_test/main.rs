struct Foo { val: i32 }
fn TakeOwnership[T:! type](input: T) {}
fn use_ref[T:! type](x: &T) -> i32 { 0 }
fn main() -> i32 {
    let a = make Foo { val: 5 };
    use_ref(&a);
    TakeOwnership(a);
    5
}
