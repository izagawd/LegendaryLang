struct Inner { val: i32 }
struct Outer { inner: Inner }

fn shared_nested(o: &Outer) {
    o.inner.val = 99;
}

fn main() -> i32 {
    let o = make Outer { inner: make Inner { val: 0 } };
    shared_nested(&o);
    o.inner.val
}
