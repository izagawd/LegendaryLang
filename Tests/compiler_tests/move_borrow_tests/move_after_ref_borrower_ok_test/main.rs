struct Foo { val: i32 }
fn TakeOwnership[T:! type](input: T) {}
fn main() -> i32 {
    let a = make Foo { val: 5 };
    let r = &a;
    let v = r.val;
    TakeOwnership(a);
    v
}
