trait MetaSized {
    let Metadata :! Sized +Copy;
}

trait Sized: MetaSized {}

trait Copy: Sized {}
impl Copy for i32 {}
impl Copy for bool {}
impl Copy for u8 {}
impl Copy for usize {}
impl Copy for () {}
impl[T:! type] Copy for &T {}
impl[T:! type] Copy for &mut T {}

impl[T:! type] Copy for *shared T {}
impl[T:! type] Copy for *mut T {}

trait MutReassign {}
impl MutReassign for i32 {}
impl MutReassign for bool {}
impl MutReassign for u8 {}
impl MutReassign for usize {}
impl MutReassign for () {}
impl[T:! type] MutReassign for &T {}
impl[T:! type] MutReassign for &mut T {}
impl[T:! type] MutReassign for *shared T {}
impl[T:! type] MutReassign for *mut T {}

trait Primitive {}
impl Primitive for i32 {}
impl Primitive for u8 {}
impl Primitive for usize {}
impl Primitive for bool {}
