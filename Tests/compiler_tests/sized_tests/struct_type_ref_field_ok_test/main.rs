struct RefBox['a](T:! Sized) { inner: &'a T }
fn peek['a, T:! Sized](rb: RefBox['a](T)) -> &'a T { rb.inner }
fn main() -> i32 {
    let v: i32 = 77;
    let rb = make RefBox { inner: &v };
    *peek(rb)
}
