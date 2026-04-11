use Std.Fmt.ToString;
use Std.Deref.Receiver;
use Std.Deref.Deref;

struct Wrapper(T:! type) {
    val: T
}

impl[T:! type] Receiver for Wrapper(T) {
    let Target :! type = T;
}

impl[T:! type] Deref for Wrapper(T) {
    fn deref(self: &Self) -> &T {
        &self.val
    }
}

impl[T:! ToString] ToString for Wrapper(T) {
    fn ToString(self: &Self) -> &str {
        T.ToString(self.deref())
    }
}

fn main() -> i32 {
    0
}
