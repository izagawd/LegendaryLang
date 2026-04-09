use Std.Ops.TryInto;
use Std.Primitive.TryCastPrimitive;

fn main() -> i32 {
    let x: i32 = 0 - 10;
    let r1: Option(u8) = x.TryInto();
    let r2: Option(usize) = x.TryInto();
    let result = 0;
    match r1 { Option.Some(v) => {}, Option.None => { result = result + 1; } };
    match r2 { Option.Some(v) => {}, Option.None => { result = result + 10; } };
    result
}
