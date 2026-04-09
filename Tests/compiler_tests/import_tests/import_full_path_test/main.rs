struct Wrapper {
    inner: i32
}

impl Std.Ops.Add(Wrapper) for Wrapper {
    let Output :! type = i32;
    fn Add(lhs: Wrapper, rhs: Wrapper) -> i32 {
        lhs.inner + rhs.inner
    }
}

impl Copy for Wrapper {}

fn main() -> i32 {
    let a = make Wrapper { inner: 21 };
    let b = make Wrapper { inner: 21 };
    a + b
}
