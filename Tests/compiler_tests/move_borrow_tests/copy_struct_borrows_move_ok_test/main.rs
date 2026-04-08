struct Foo { kk: i32 }

struct CopyHolder['a] { r: &'a Foo }
impl Copy for CopyHolder {}

fn TakeOwnership[T:! type](input: T) {}

fn main() -> i32 {
    let a = make Foo { kk: 5 };
    let ch = make CopyHolder { r: &a };
    let val = ch.r.kk;
    TakeOwnership(a);
    val
}
