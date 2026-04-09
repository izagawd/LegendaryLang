use Std.Ops.Add;

struct Inner(T:! Add(T)) {
    val: (T as Add(T)).Output
}

struct Outer(T:! Add(T)) {
    inner: Inner(T)
}

fn main() -> i32 {
    let o = make Outer { inner: make Inner { val: 1 + 2 } };
    o.inner.val
}
